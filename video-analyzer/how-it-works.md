# Video Analyzer - How It Works

## Table of Contents
- [Overview](#overview)
- [Architecture](#architecture)
- [RekaVisionService Deep Dive](#rekavisionservice-deep-dive)
  - [Responsibilities](#responsibilities)
  - [Dependency Injection & Configuration](#dependency-injection--configuration)
  - [Authentication Header](#authentication-header)
  - [HTTP Pipeline Helper Methods](#http-pipeline-helper-methods)
  - [Core Operations](#core-operations)

## Overview

Video Analyzer is a **Blazor Server** application built with **.NET 10.0** that leverages the **Reka AI Vision API** to provide intelligent video analysis capabilities. The application enables users to upload videos, perform semantic searches across video content, ask questions about specific videos, and generate study materials or presentation evaluations.

The application uses a clean architecture with separation of concerns between the UI components (Blazor Razor components), business logic (Services), and domain models.

## Architecture

### Technology Stack

- **Framework**: ASP.NET Core 10.0 (Blazor Server with Interactive Server rendering)
- **UI Library**: Microsoft Fluent UI Components
- **Markdown Processing**: Markdig
- **API Integration**: Reka AI Vision API
- **Containerization**: Docker/Podman support

### Project Structure

```text
video-analyzer/
├── src/
│   ├── Program.cs                   # Application entry point and service configuration
│   ├── Components/
│   │   ├── Pages/                   # Main pages (VideoManagement, Home)
│   │   ├── Shared/                  # Reusable components
│   │   ├── Dialog/                  # Modal dialogs (AddVideoDialog)
│   │   └── Layout/                  # Layout components
│   ├── Services/                    # Business logic and API integration
│   │   ├── IRekaVisionService.cs    # Service interface
│   │   └── RekaVisionService.cs     # Reka API implementation
│   ├── Domain/                      # Domain models and DTOs
│   │   ├── Video.cs                 # Video entity
│   │   ├── SearchResult.cs          # Search result model
│   │   ├── QAAnswer.cs              # Q&A response model
│   │   └── IndexingStatus.cs        # Video indexing states
│   └── wwwroot/                     # Static assets
├── Dockerfile                        # Container configuration
├── docker-compose.yml               # Multi-container orchestration
└── appsettings.json                 # Application configuration
```


## RekaVisionService Deep Dive

### Responsibilities

`RekaVisionService` encapsulates all communication with the external Reka Vision REST API. It:

- Normalizes configuration (API key, base URL)
- Builds and sends authenticated HTTP requests
- Translates API JSON payloads into domain models (`Video`, `SearchResult`, `QAAnswer`)
- Provides a thin, intention‑revealing API surface for the UI layer
- Centralizes error handling and logging

### Dependency Injection & Configuration

The service is registered as a typed/regular service (see `Program.cs`) and receives its dependencies via constructor injection:

```csharp
public RekaVisionService(HttpClient httpClient, ILogger<RekaVisionService> logger, IConfiguration configuration)
{
    _httpClient = httpClient;
    _logger = logger;
    _rekaAPIKey = Environment.GetEnvironmentVariable("REKA_API_KEY")
                    ?? configuration["RekaAPIKey"]
                    ?? throw new ArgumentException("RekaAPIKey configuration is required...");
    _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
}
```

Order of precedence for the API key:
 
1. `REKA_API_KEY` environment variable (ideal for container / CI secret management)
2. `RekaAPIKey` in `appsettings*.json`
 

### Authentication Header

Every outbound request includes the provider‑specific header produced by `CreateRequest`:

```csharp
private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
{
    var request = new HttpRequestMessage(method, endpoint);
    request.Headers.Add("X-Api-Key", _rekaAPIKey);
    return request;
}
```

### HTTP Pipeline Helper Methods

All network I/O funnels through `SendRequestAsync` which centralizes:

```csharp
private async Task<string> SendRequestAsync(HttpRequestMessage request, string operationName)
{
    try
    {
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
    catch (HttpRequestException ex)
    {
        throw new InvalidOperationException($"Failed to {operationName} from Reka Vision API", ex);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error occurred while {Operation}", operationName);
        throw;
    }
}
```

### Core Operations

#### 1. List Videos

```csharp
public async Task<List<Video>> GetAllVideos()
{
    var request = CreateRequest(HttpMethod.Post, $"{BaseEndpoint}/videos/get");
    request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
    var responseContent = await SendRequestAsync(request, "fetch videos");
    var rekaResponse = JsonSerializer.Deserialize<RekaVideoResponse>(responseContent, _jsonOptions);
    return rekaResponse?.Results?.Select(ConvertToVideo).ToList() ?? [];
}
```

Notes:

- API uses POST (not GET) for retrieval — the service abstracts that quirk.
- Defensive null handling returns an empty list rather than throwing.

#### 2. Upload & Index

```csharp
public async Task<Video> AddVideo(string videoUrl, string videoName)
{
    var request = CreateRequest(HttpMethod.Post, $"{BaseEndpoint}/videos/upload");
    var formData = new MultipartFormDataContent
    {
        { new StringContent(videoUrl), "video_url" },
        { new StringContent(videoName), "video_name" },
        { new StringContent("true"), "index" }
    };
    request.Content = formData;
    var json = await SendRequestAsync(request, $"upload video {videoName}");
    var dto = JsonSerializer.Deserialize<RekaVideoUploadResponse>(json, _jsonOptions)
              ?? throw new InvalidOperationException("Failed to parse upload response");
    return new Video { VideoId = Guid.Parse(dto.VideoId!), Url = videoUrl, IndexingStatus = ParseIndexingStatus(dto.Status) };
}
```

Multipart form usage avoids loading binary content here because we pass a URL rather than raw bytes; switching to file streams would only require adding `StreamContent` parts.
