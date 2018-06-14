using CurlToSharp.Services;

using Microsoft.Extensions.DependencyInjection;

namespace CurlToSharp.Infrastructure
{
    public static class IocExtensions
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            services.AddSingleton<ICommandLineParser, CommandLineParser>();
            services.AddSingleton<IConverterService, ConverterService>();
        }
    }
}
