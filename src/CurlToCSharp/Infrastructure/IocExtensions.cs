using Curl.Converter.Net;
using Curl.Parser.Net;

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

        services.AddSingleton<IParser, Parser>();
        services.AddSingleton<IConverter, Converter>();
    }
}
