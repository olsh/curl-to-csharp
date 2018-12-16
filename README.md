# curl to C#
[![Build status](https://ci.appveyor.com/api/projects/status/rfdgvqb9x0dwddy8?svg=true)](https://ci.appveyor.com/project/olsh/curl-to-csharp)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=curl-to-csharp&metric=alert_status)](https://sonarcloud.io/dashboard?id=curl-to-csharp)

This ASP.NET Core app converts curl commands to C# code

## Demo

https://curl.olsh.me

## Run locally

You can grab latest binaries [here](https://ci.appveyor.com/project/olsh/curl-to-csharp/build/artifacts) and run `dotnet CurlToCSharp.dll`

## Build

1. Install cake

`dotnet tool install -g Cake.Tool --version 0.31.0`

2. Run build

`dotnet cake build.cake`
