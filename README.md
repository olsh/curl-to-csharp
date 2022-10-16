# curl to C#
[![Build status](https://ci.appveyor.com/api/projects/status/rfdgvqb9x0dwddy8?svg=true)](https://ci.appveyor.com/project/olsh/curl-to-csharp)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=curl-to-csharp&metric=alert_status)](https://sonarcloud.io/dashboard?id=curl-to-csharp)
[![Docker pulls](https://img.shields.io/docker/pulls/olsh/curl-to-csharp)](https://hub.docker.com/r/olsh/curl-to-csharp)

This ASP.NET Core app converts curl commands to C# code

## Demo

https://curl.olsh.me

## Telegram bot

https://t.me/curl_to_csharp_bot

## Run with docker

1. Run container

```bash
docker run -p 8080:80 olsh/curl-to-csharp
```

2. Open http://localhost:8080

## Run locally

You can grab latest binaries [here](https://ci.appveyor.com/project/olsh/curl-to-csharp/build/artifacts) and run `dotnet CurlToCSharp.dll`

## Build

1. Install cake

```bash
dotnet tool install -g Cake.Tool
```

2. Run build

```bash
dotnet cake build.cake
```

## NuGet Packages
### Curl.CommandLine.Parser
[![NuGet](https://img.shields.io/nuget/v/Curl.CommandLine.Parser.svg)](https://www.nuget.org/packages/Curl.CommandLine.Parser/)

#### Key Features
- Parses cURL command into individual cURL options.
- Returns parsing errors and warnings if the cURL input is invalid.

#### Installation
Install with NuGet
```cmd
dotnet add package Curl.CommandLine.Parser
```

#### Usage/Examples
```c#
var input = @"curl https://sentry.io/api/0/projects/1/groups/?status=unresolved -d '{""status"": ""resolved""}' -H 'Content-Type: application/json' -u 'username:password' -H 'Accept: application/json' -H 'User-Agent: curl/7.60.0'";

var output = new CurlParser(new ParsingOptions() { MaxUploadFiles = 10 }).Parse(input);

Console.WriteLine(output.Data.UploadData.First().Content);
// Output:
// {"status": "resolved"}
```

### Curl.HttpClient.Converter
[![NuGet](https://img.shields.io/nuget/v/Curl.HttpClient.Converter.svg)](https://www.nuget.org/packages/Curl.HttpClient.Converter/)
#### Key Features
- Converts output from CurlParser into C# code.
- Returns parsing errors and warnings if the cURL input is invalid.

#### Installation
Install with NuGet
```cmd
dotnet add package Curl.HttpClient.Converter
```

#### Usage/Examples
```c#
var input = @"curl https://sentry.io/api/0/projects/1/groups/?status=unresolved -d '{""status"": ""resolved""}' -H 'Content-Type: application/json' -u 'username:password' -H 'Accept: application/json' -H 'User-Agent: curl/7.60.0'";
var curlOption = new CurlParser().Parse(input);
var output = new CurlHttpClientConverter().ToCsharp(curlOption.Data);
// Output:
/*
// In production code, don't destroy the HttpClient through using, but better reuse an existing instance
// https://www.aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
using (var httpClient = new HttpClient())
{
    using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://sentry.io/api/0/projects/1/groups/?status=unresolved"))
    {
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("User-Agent", "curl/7.60.0");

        var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes("username:password"));
        request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");

        request.Content = new StringContent("{\"status\": \"resolved\"}");
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

        var response = await httpClient.SendAsync(request);
    }
}
*/
```
