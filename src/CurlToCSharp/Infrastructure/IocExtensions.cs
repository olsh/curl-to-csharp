using Curl.HttpClient.Converter;
using Curl.CommandLine.Parser;

using CurlToCSharp.Models;

using Microsoft.Extensions.Options;

namespace CurlToCSharp.Infrastructure;

public static class IocExtensions
{
    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton(
            provider => provider.GetService<IOptions<ApplicationOptions>>()
                .Value.Parsing);

        services.AddSingleton<ICurlParser, CurlParser>();
        services.AddSingleton<ICurlConverter, CurlConverter>();
    }
}
