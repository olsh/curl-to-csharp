using CurlToCSharp.Models;
using CurlToCSharp.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CurlToCSharp.Infrastructure
{
    public static class IocExtensions
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            services.AddSingleton(
                provider => provider.GetService<IOptions<ApplicationOptions>>()
                    .Value.Parsing);

            services.AddSingleton<ICommandLineParser, CommandLineParser>();
            services.AddSingleton<IConverterService, ConverterService>();
        }
    }
}
