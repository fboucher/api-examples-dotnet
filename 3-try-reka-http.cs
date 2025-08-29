
#:package DotNetEnv@3.1.1

using DotNetEnv;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

Env.Load();

var REKA_API_KEY = Environment.GetEnvironmentVariable("REKA_API_KEY")!;

using var httpClient = new HttpClient();
httpClient.Timeout = Timeout.InfiniteTimeSpan;

var baseUrl = "http://api.reka.ai/v1/chat/completions";

var requestPayload = new
{
    model = "reka-flash-research",
    messages = new[]
            {
                new
                {
                    role = "user",
                    content = "Give me 3 nice, not crazy expensive, restaurants for a romantic dinner in New York city"
                }
            }
};

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
};

var jsonPayload = JsonSerializer.Serialize(requestPayload, jsonOptions);

using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl);
request.Headers.Add("Authorization", $"Bearer {REKA_API_KEY}");
request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

try
{
    var response = await httpClient.SendAsync(request);

    var responseContent = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        var jsonDocument = JsonDocument.Parse(responseContent);

        var contentString = jsonDocument.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        Console.WriteLine(contentString);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
