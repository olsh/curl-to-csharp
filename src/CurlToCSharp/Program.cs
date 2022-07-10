namespace CurlToCSharp;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await CreateWebHostBuilder(args)
            .Build()
            .RunAsync();
    }

    private static IHostBuilder CreateWebHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(builder =>
            {
                builder.UseStartup<Startup>();
            });
    }
}
