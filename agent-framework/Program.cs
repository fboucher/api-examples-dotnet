
    // <PackageReference Include="DotNetEnv" Version="3.1.1" />
    // <PackageReference Include="Microsoft.Agents.AI" Version="1.0.0-preview.251002.1" />
    // <PackageReference Include="Microsoft.Agents.AI.OpenAI" Version="1.0.0-preview.251002.1" />

using System.ClientModel;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

Env.TraversePath().Load();
var apiKey = Environment.GetEnvironmentVariable("APIKEY") ?? throw new InvalidOperationException("APIKEY is not set.");
var model = "reka-flash-research";
var endpoint = "http://api.reka.ai/v1";

ChatClientAgentOptions agentOptions = new(instructions: "You are good at telling jokes.", name: "Joker")
{
    ChatOptions = new()
    {
        ResponseFormat = ChatResponseFormat.ForJsonSchema<JokeInfo>()
    }
};

AIAgent agent = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
	.GetChatClient(model)
	.CreateAIAgent(agentOptions);

Console.WriteLine($"Let's ask for a joke...\n");

JokeInfo jokeInfo;
try
{
	var response = await agent.RunAsync("Tell me a geek joke.");
	jokeInfo = response.Deserialize<JokeInfo>(JsonSerializerOptions.Web);
}
catch (Exception ex)
{
	Console.WriteLine($"Error: {ex.Message}");
	return;
}


Console.WriteLine($"Joke: {jokeInfo.Joke}");
Console.WriteLine($"Category: {jokeInfo.Category}");
Console.WriteLine($"Source: {jokeInfo.source}");


[Description("A joke with its category and source.")]
public class JokeInfo
{
	[JsonPropertyName("joke-text")]
	public string Joke { get; set; } = string.Empty;

	[JsonPropertyName("category")]
	public string Category { get; set; } = string.Empty;

	[JsonPropertyName("source")]
	public string source { get; set; } = string.Empty;
}