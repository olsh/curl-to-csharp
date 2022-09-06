# Curl.Parser.Net
[![Build status](https://ci.appveyor.com/api/projects/status/rfdgvqb9x0dwddy8?svg=true)](https://ci.appveyor.com/project/olsh/curl-to-csharp)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=curl-to-csharp&metric=alert_status)](https://sonarcloud.io/dashboard?id=curl-to-csharp)
[![Docker pulls](https://img.shields.io/docker/pulls/olsh/curl-to-csharp)](https://hub.docker.com/r/olsh/curl-to-csharp)

A cURL parser based on [curl-to-csharp](https://github.com/olsh/curl-to-csharp) project for .NET.

Supported platforms:
- For ASP.NET 6, .NET 6
- For ASP.NET 5, .NET 5
- For ASP.NET Core 3, .NET Core 3.0
- For ASP.NET Core 2.1, .NET Core 2.1

## Key Features
- Parse cURL command into individual cURL options.
- Return parsing errors and warnings if the cURL input is invalid.

## Installation
Install with NuGet
```cmd
dotnet add package Curl.Parser.Net
```

## Usage/Examples
```c#
var input = @"curl https://sentry.io/api/0/projects/1/groups/?status=unresolved -d '{""status"": ""resolved""}' -H 'Content-Type: application/json' -u 'username:password' -H 'Accept: application/json' -H 'User-Agent: curl/7.60.0'";

var output = new Parser(new ParsingOptions() { MaxUploadFiles = 10 }).Parse(input);

Console.WriteLine(output.Data.UploadData.First().Content);
// Output:
// {"status": "resolved"}
```
