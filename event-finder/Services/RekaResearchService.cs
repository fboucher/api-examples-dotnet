using System;
using System.IO;
using System.Text;
using System.Text.Json;
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
                var answer = rekaResponse!.Choices![0]!.Message!.ParsedContent<EventsResponse>()?.Events;
                eventResponse.Events = answer ?? new List<TechEvent>();
                eventResponse.ReasoningSteps = rekaResponse.Choices[0].Message!.ReasoningSteps ?? new List<ReasoningStep>();
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
