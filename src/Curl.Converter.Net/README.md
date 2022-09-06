# Curl.Converter.Net
[![Build status](https://ci.appveyor.com/api/projects/status/rfdgvqb9x0dwddy8?svg=true)](https://ci.appveyor.com/project/olsh/curl-to-csharp)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=curl-to-csharp&metric=alert_status)](https://sonarcloud.io/dashboard?id=curl-to-csharp)
[![Docker pulls](https://img.shields.io/docker/pulls/olsh/curl-to-csharp)](https://hub.docker.com/r/olsh/curl-to-csharp)

A cURL parser and C# code converter based on [curl-to-csharp](https://github.com/olsh/curl-to-csharp) project for .NET.

Supported platforms:
- For ASP.NET 6, .NET 6
- For ASP.NET 5, .NET 5
- For ASP.NET Core 3, .NET Core 3.0
- For ASP.NET Core 2.1, .NET Core 2.1

## Key Features
- Parse cURL command into C# code.
- Convert output from CurlParser into C# code.
- Return parsing errors and warnings if the cURL input is invalid.

## Installation
Install with NuGet
```cmd
dotnet add package Curl.Converter.Net
```

## Usage/Examples
```c#
var input = @"curl https://sentry.io/api/0/projects/1/groups/?status=unresolved -d '{""status"": ""resolved""}' -H 'Content-Type: application/json' -u 'username:password' -H 'Accept: application/json' -H 'User-Agent: curl/7.60.0'";

var output = new Converter().Parse(input, 10);

Console.WriteLine(output.Data);
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
