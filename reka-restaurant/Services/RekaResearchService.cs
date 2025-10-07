using System;
using System.IO;
using System.Text;
using System.Text.Json;
using web.Domain;

namespace web.Services;

public class RekaResearchService(HttpClient httpClient, IConfiguration config, ILogger<RekaResearchService> logger)
{
    private readonly HttpClient _http = httpClient;
    private readonly string _apiKey = config["AppSettings:REKA_API_KEY"] ?? Environment.GetEnvironmentVariable("REKA_API_KEY") ?? throw new InvalidOperationException("REKA_API_KEY environment variable is not set.");
    private readonly ILogger<RekaResearchService> _logger = logger;


    public async Task<RestaurantResponse> GetRestaurantReferences(string mood, string nearCity)
    {
        //var requestUrl = "http://localhost:5085/research";
        var requestUrl = "https://api.reka.ai/v1/chat/completions";

        var query = $"You are a restaurant recommender. User ask for {mood}. Provide Find 3 restaurants that match this mood. Always respond as JSON that matches the provided schema.";

        var restaurantResponse = new RestaurantResponse();

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
                web_search = new
                {
                    //allowed_domains = new string[] { "tripadvisor.com" },
                    blocked_domains = new string[] { "ubereats.com" },
                    max_uses = 4,
                    user_location = new
                    {
                        approximate = new
                        {
                            city = nearCity

                        }
                    }
                }
            },
        };

        var jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger.LogInformation($"Request Payload: {jsonPayload}");


        await SaveToFile("request", mood, jsonPayload ?? string.Empty);

        HttpResponseMessage? response = null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            response = await _http.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            await SaveToFile(mood, nearCity, responseContent);

            var rekaResponse = JsonSerializer.Deserialize<RekaResponse>(responseContent);

            if (response.IsSuccessStatusCode)
            {
                var answerStr = rekaResponse!.Choices![0]!.Message!.ParsedContent?.Restaurants;
                restaurantResponse.Restaurants = answerStr ?? new List<Restaurant>();
                restaurantResponse.ReasoningSteps = rekaResponse.Choices[0].Message!.ReasoningSteps ?? new List<ReasoningStep>();
            }
            else
            {
                throw new Exception($"Request failed with status code: {response.StatusCode}. Response: {responseContent}");
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError($"Oops! Exception occurred while fetching restaurant references. Details: {ex.Message}");
        }

        return restaurantResponse;
    }


    private async Task SaveToFile(string mood, string city, string responseContent)
    {
        string datetime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
        string fileName = $"{mood}_{city}_{datetime}.json";
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
                name = "ListRestaurants",
                schema = new
                {
                    type = "object",
                    properties = new
                    {
                        restaurants = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    name = new { type = "string" },
                                    address = new { type = "string" },
                                    phoneNumber = new { type = "string" },
                                    website = new { type = "string" },
                                    score = new { type = "integer" },
                                    priceLevel = new
                                    {
                                        type = "string",
                                        @enum = new[] { "$", "$$", "$$$" }
                                    }
                                },
                                required = new[] { "name", "address", "phoneNumber", "website", "score", "priceLevel" }
                            }
                        }
                    },
                    required = new[] { "restaurants" }
                }
            }
        };
    }
}
