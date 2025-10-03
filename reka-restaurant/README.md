# Reka Restaurant Finder

This demo showcases how to use the **Reka Research** to build intelligent apps that can search the web, structure responses, and support reasoning. It’s designed to help developers learn how to integrate and use Reka Research and use the advanced features.

> Requirement:
>
> - Have .NET 9 installed or Docker or CodeSpace

If you don't have .NET install on you computer use the devContainer with [Docker](https://code.visualstudio.com/docs/devcontainers/tutorial) or [CodeSpace](https://docs.github.com/en/codespaces/quickstart) to create your workspace.

## 1. Get your API key

1) Go to the [Reka Platform dashboard](https://link.reka.ai/free)
2) Open the API Keys section on the left
3) Create a new key and copy it to your environment
4) Add the key into [appsettings.json)](appsettings.json) (or appsettings.Development.json).

Voilà! Your are all set!

## 2. Run the demo

1) From the `reka-restaurant` folder, execute `dotnet run`.

## 3. Modify the code

Open the project in your favorite IDE/ editor and you can look at [Services/RekaResearchService.cs](Services/RekaResearchService.cs) and change different parameters to see how it works.

- `allowed_domains` or `blocked_domains`
- `response_format` to control the output
- `max_uses` to limit the number of searches

## References

- [Docs: Reka Research API](https://docs.reka.ai/research)
- [Blog Post: How to Leverage Reka Research to Build Smarter AI Apps](https://reka.ai/news/how-to-leverage-reka-research-to-build-smarter-ai-apps)
- [Discord](https://link.reka.ai/discord)

