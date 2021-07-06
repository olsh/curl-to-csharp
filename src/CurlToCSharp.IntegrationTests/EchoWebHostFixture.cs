using System;
using System.Threading;
using System.Threading.Tasks;

using CurlToCSharp.IntegrationTests.Constants;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace CurlToCSharp.IntegrationTests
{
    internal class EchoWebHostFixture : IDisposable
    {
        private readonly IWebHost _webHost;

        public EchoWebHostFixture()
        {
            _webHost = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseUrls(WebHostConstants.TestServerHost)
                .Build();

            Task.Run(() => _webHost.Run());

            // What a second while server startup
            Thread.Sleep(1000);
        }

        public void Dispose()
        {
            _webHost?.Dispose();
        }
    }
}
