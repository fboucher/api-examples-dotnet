using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using event_finder.Domain;
using Reka.SDK;

namespace event_finder.Services;

public class RekaResearchService(HttpClient httpClient, IConfiguration config, ILogger<RekaResearchService> logger)
{
    private readonly HttpClient _http = httpClient;
    private readonly string _apiKey = config["AppSettings:REKA_API_KEY"] ?? Environment.GetEnvironmentVariable("REKA_API_KEY") ?? throw new InvalidOperationException("REKA_API_KEY environment variable is not set.");
    private readonly ILogger<RekaResearchService> _logger = logger;


    public async Task<EventResponse> GetEventReferences(string topic, UserLocationApproximate? userLocationApproximate, string[]? allowedDomains = null, string[]? blockedDomains = null)
    {
        //var requestUrl = "http://localhost:5085/research";
        var requestUrl = "https://api.reka.ai/v1/chat/completions";

        var minDate = DateTime.UtcNow.Date.AddMonths(1).ToString("yyyy-MM-dd");
        var query = $"You are a tech events recommender. The user is interested in {topic}. Find 3 upcoming tech events related to this topic occurring after {minDate}. Exclude any past events. Always respond as JSON that matches the provided schema.";

        var eventResponse = new EventResponse();

        var webSearch = new Dictionary<string, object>
        {
            ["max_uses"] = 3
        };

        var approximate = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(userLocationApproximate?.Town))
        {
            approximate["city"] = userLocationApproximate.Town;
        }
        if (!string.IsNullOrEmpty(userLocationApproximate?.Region))
        {
            approximate["region"] = userLocationApproximate.Region;
        }
        if (!string.IsNullOrEmpty(userLocationApproximate?.Country))
        {
            approximate["country"] = userLocationApproximate.Country;
        }

        if (approximate.Count > 0)
        {
            webSearch["user_location"] = new { approximate };
        }

        if (allowedDomains != null && allowedDomains.Length > 0)
        {
            webSearch["allowed_domains"] = allowedDomains;
        }

        if (blockedDomains != null && blockedDomains.Length > 0)
        {
            webSearch["blocked_domains"] = blockedDomains;
        }

        var requestPayload = new
        {
            model = "reka-flash-research",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = query
                }
            },

            response_format = GetResponseFormat(),
            research = new
            {
                web_search = webSearch
            },
        };

        var jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger.LogInformation($"Request Payload: {jsonPayload}");


        await SaveToFile("request", jsonPayload ?? string.Empty);

        HttpResponseMessage? response = null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = new StringContent(jsonPayload!, Encoding.UTF8, "application/json");

            response = await _http.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            await SaveToFile("response", responseContent);

            var rekaResponse = JsonSerializer.Deserialize<RekaResponse>(responseContent);

            if (response.IsSuccessStatusCode)
            {
                var message = rekaResponse!.Choices![0]!.Message!;
                eventResponse.Events = ParseEventsWithFallback(message);
                eventResponse.ReasoningSteps = message.ReasoningSteps ?? new List<ReasoningStep>();
            }
            else
            {
                throw new Exception($"Request failed with status code: {response.StatusCode}. Response: {responseContent}");
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError($"Oops! Exception occurred while fetching tech event references. Details: {ex.Message}");
        }

        return eventResponse;
    }

    private List<TechEvent> ParseEventsWithFallback(dynamic message)
    {
        try
        {
            return message.ParsedContent<EventsResponse>()?.Events ?? new List<TechEvent>();
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning("Structured parse failed, trying recovery from message.content. Details: {Message}", jsonEx.Message);
        }

        string? content = message.Content?.ToString();
        if (string.IsNullOrWhiteSpace(content))
        {
            return new List<TechEvent>();
        }

        if (TryDeserializeEvents(content, out var events))
        {
            return events;
        }

        var repairedJson = RepairPotentiallyMalformedJson(content);
        if (TryDeserializeEvents(repairedJson, out events))
        {
            _logger.LogInformation("Recovered event parsing from repaired JSON content.");
            return events;
        }

        _logger.LogWarning("Unable to parse event list from assistant message content after recovery attempts.");
        return new List<TechEvent>();
    }

    private static bool TryDeserializeEvents(string json, out List<TechEvent> events)
    {
        events = new List<TechEvent>();

        try
        {
            var parsed = JsonSerializer.Deserialize<EventsResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            events = parsed?.Events ?? new List<TechEvent>();
            return parsed?.Events is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string RepairPotentiallyMalformedJson(string content)
    {
        var json = content.Trim();

        if (json.StartsWith("```") && json.EndsWith("```"))
        {
            json = Regex.Replace(json, "^```(?:json)?\\s*", string.Empty, RegexOptions.IgnoreCase);
            json = Regex.Replace(json, "\\s*```$", string.Empty);
        }

        var firstBrace = json.IndexOf('{');
        if (firstBrace >= 0)
        {
            json = json[firstBrace..];
        }

        json = Regex.Replace(json, @",(\s*[\]}])", "$1");

        int openObjects = 0;
        int openArrays = 0;
        bool inString = false;
        bool escaped = false;

        foreach (var ch in json)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (ch == '\\')
            {
                escaped = true;
                continue;
            }

            if (ch == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
            {
                continue;
            }

            if (ch == '{') openObjects++;
            else if (ch == '}' && openObjects > 0) openObjects--;
            else if (ch == '[') openArrays++;
            else if (ch == ']' && openArrays > 0) openArrays--;
        }

        if (openArrays > 0)
        {
            json += new string(']', openArrays);
        }

        if (openObjects > 0)
        {
            json += new string('}', openObjects);
        }

        return json;
    }


    private async Task SaveToFile(string prefix, string responseContent)
    {
        string datetime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
        string fileName = $"{prefix}_{datetime}.json";
        string folderPath = "Data";
        Directory.CreateDirectory(folderPath);
        string filePath = Path.Combine(folderPath, fileName);
        await File.WriteAllTextAsync(filePath, responseContent);
    }


    private object GetResponseFormat()
    {
        return new
        {
            type = "json_schema",
            json_schema = new
            {
                name = "ListEvents",
                schema = new
                {
                    type = "object",
                    properties = new
                    {
                        events = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    name = new { type = "string" },
                                    startDate = new { type = "string" },
                                    endDate = new { type = "string" },
                                    city = new { type = "string" },
                                    country = new { type = "string" },
                                    url = new { type = "string" }
                                },
                                required = new[] { "name", "startDate", "endDate", "city", "country", "url" }
                            }
                        }
                    },
                    required = new[] { "events" }
                }
            }
        };
    }
}
