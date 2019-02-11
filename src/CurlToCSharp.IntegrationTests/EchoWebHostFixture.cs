using System;
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
        }

        public void Dispose()
        {
            _webHost?.Dispose();
        }
    }
}
