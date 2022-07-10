using CurlToCSharp.IntegrationTests.Constants;

namespace CurlToCSharp.IntegrationTests;

internal class EchoWebHostFixture : IDisposable
{
    private readonly IHost _webHost;

    public EchoWebHostFixture()
    {
        _webHost = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(builder =>
            {
                builder.UseStartup<Startup>();
                builder.UseKestrel(options =>
                {
                    options.ListenLocalhost(WebHostConstants.TestServerPort);
                });
            })
            .Build();

        _webHost.RunAsync();

        // What a second while server startup
        Thread.Sleep(1000);
    }

    public void Dispose()
    {
        _webHost?.Dispose();
    }
}
