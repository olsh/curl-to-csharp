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
            services.AddSingleton<ICommandLineParser>(provider => new CommandLineParser(provider.GetService<IOptions<ApplicationOptions>>().Value.Parsing));
            services.AddSingleton<IConverterService, ConverterService>();
        }
    }
}
