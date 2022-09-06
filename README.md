# curl to C#
[![Build status](https://ci.appveyor.com/api/projects/status/rfdgvqb9x0dwddy8?svg=true)](https://ci.appveyor.com/project/olsh/curl-to-csharp)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=curl-to-csharp&metric=alert_status)](https://sonarcloud.io/dashboard?id=curl-to-csharp)
[![Docker pulls](https://img.shields.io/docker/pulls/olsh/curl-to-csharp)](https://hub.docker.com/r/olsh/curl-to-csharp)

This ASP.NET Core app converts curl commands to C# code

## Demo

https://curl.olsh.me

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
