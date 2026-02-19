
#:package DotNetEnv@3.1.1
#:package OpenAI@2.8.0

using DotNetEnv;
using OpenAI.Chat;
using OpenAI;
using System.ClientModel;
using OpenAI.Responses;

Env.TraversePath().Load();

var API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
var baseUrl = "https://api.openai.com/v1";
var modelName = "gpt-5";

// == OnlY works with OpenAI
// var API_KEY = Environment.GetEnvironmentVariable("REKA_API_KEY")!;
// var baseUrl = "https://api.reka.ai/v1";
// var modelName = "reka-flash-research";

// var API_KEY = "ollama";
// var baseUrl = "http://192.168.2.11:11434/v1/";
// var modelName = "llama3.1:8b";

string[] allowedDomains =
[
    "sessionize.com",
    "microsoft.com",
    "github.com",
    "nvidia.com"
];

string prompt = "Suggest 3 tech event, with a AI focus, that I can attend";

// ============================
// Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable OPENAI001 

var openAiClient = new ResponsesClient(modelName, new ApiKeyCredential(API_KEY), new OpenAIClientOptions
                        {
                            Endpoint = new Uri(baseUrl)
                        });


var filters = new WebSearchToolFilters();
foreach (var domain in allowedDomains)
    filters.AllowedDomains.Add(domain);

var webSearchTool = ResponseTool.CreateWebSearchTool(
    filters: filters,
    searchContextSize: WebSearchToolContextSize.High   // Low | Medium | High
    // Optional — localise results:
    // userLocation: WebSearchToolLocation.CreateApproximateLocation(
    //     country: "US", city: "New York", region: "NY", timezone: "America/New_York")
);

var options = new CreateResponseOptions();
options.Tools.Add(webSearchTool);
options.InputItems.Add(ResponseItem.CreateUserMessageItem(prompt));

#pragma warning restore OPENAI001 

// Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
// ============================

var response = await openAiClient.CreateResponseAsync(options);
var result = response.Value;


Console.WriteLine(result.GetOutputText());