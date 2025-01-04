# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# we can't copy to a directory because WORKDIR doesn't work in buildx
# https://github.com/docker/buildx/issues/378
COPY / /

ENTRYPOINT ["dotnet", "CurlToCSharp.dll"]
