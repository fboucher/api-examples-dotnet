# C# Script demos with Reka API

Reka API's are compatible with open AI that made them very easy to swap and be used because it's a very common structure this represents to read you will see multiple example that use different different APIs and different and different scenario while using the latest feature of.net that is script. Script is a very fun feature where you don't need a project to have a program written in C#.

![screen capture](../assets/capture-file-based.png)

Requirement:

- Have .NET 10 preview installed or Docker or CodeSpace

If you don't have .NET install on you computer use the devContainer with [Docker](https://code.visualstudio.com/docs/devcontainers/tutorial) or [CodeSpace](https://docs.github.com/en/codespaces/quickstart) to create your workspace.

## 1. Get your API key

1) Go to the [Reka Platform dashboard](https://link.reka.ai/free)
2) Open the API Keys section on the left
3) Create a new key and copy it to your environment
4) Rename the file `.env-sample` `.env`, and replace the place holder (*HERE_GOES_YOUR_API_KEY*) by the key you just created.

Voilà! Your are all set!

## Try Reka

There are three example in this repository doing exactly the same thing but using different method to achieve it.

- **[1-try-1-try-reka-openai](./1-try-reka-openai.cs)**: Using the OpenAI SDK
- **[2-try-reka-ms-ext](./2-try-reka-ms-ext.cs)**: Using the Microsoft Extension AI for OpenAI
- **[3-try-reka-http](./3-try-reka-http.cs)**: Using the HttpClient to call Reka API directly

To try any of them you just need to open a terminal in the folder of the example you want to try and type:

```csharp
dotnet run 1-try-reka-openai.cs
```

or

```csharp
dotnet run 2-try-reka-ms-ext.cs
```

or

```csharp
dotnet run 3-try-reka-http.cs
```

After a little while you should see three restaurant recommendation for your trip in Montreal.

## Do more

You can change the prompt to get different recommendation or you can change the location to get recommendation for another city. You can also modify the code and use more advance feature of Reka's API! You can find all the documentation on [https://docs.reka.ai/](https://docs.reka.ai/). You should join our community on [Discord](https://discord.com/invite/MTRJEBvH).

This code was used in a blog post and video that you can find here

- [Blog post](#)
- [Video](#)


