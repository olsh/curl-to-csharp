using System;
using System.Linq;

using CurlToCSharp.Services;

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

using Xunit;

namespace CurlToCSharp.Tests.Services
{
    public class CommandLineParserTests
    {
        [Fact]
        public void ParseSettings_SimpleConfiguration_DefaultMethodIsGet()
        {
            var service = new CommandLineParser();

            var parseResult = service.Parse(new Span<char>(@"curl -i https://sentry.io/api/0/".ToCharArray()));

            Assert.Equal(HttpMethod.Get, parseResult.Data.HttpMethod);
            Assert.Equal(new Uri("https://sentry.io/api/0/"), parseResult.Data.Url);
        }

        [Fact]
        public void ParseSettings_ValidConfiguration_Success()
        {
            var service = new CommandLineParser();

            var parseResult = service.Parse(new Span<char>(
                @"curl -X POST -H ""Content-Type: application/json"" -H ""Authorization: Bearer b7d03a6947b217efb6f3ec3bd3504582"" -d '{""type"":""A"",""name"":""www"",""data"":""162.10.66.0"",""priority"":null,""port"":null,""weight"":null}' ""https://api.digitalocean.com/v2/domains/example.com/records""".ToCharArray()));

            Assert.Equal(@"{""type"":""A"",""name"":""www"",""data"":""162.10.66.0"",""priority"":null,""port"":null,""weight"":null}", parseResult.Data.Payload);
            Assert.Equal(HttpMethod.Post, parseResult.Data.HttpMethod);
            Assert.Equal(new Uri("https://api.digitalocean.com/v2/domains/example.com/records"), parseResult.Data.Url);
            Assert.Equal("Bearer b7d03a6947b217efb6f3ec3bd3504582", parseResult.Data.Headers.First(g => g.Key == "Authorization").Value);
        }

        [Fact]
        public void ParseSettings_Multiline_Success()
        {
            var service = new CommandLineParser();

            var parseResult = service.Parse(new Span<char>(
                @"$ curl -i https://sentry.io/api/0/projects/1/groups/ \
                    -d '{""status"": ""resolved""}' \
                    -H 'Content-Type: application/json'".ToCharArray()));

            Assert.Equal(@"{""status"": ""resolved""}", parseResult.Data.Payload);
            Assert.Equal(HttpMethod.Post, parseResult.Data.HttpMethod);
            Assert.Equal(new Uri("https://sentry.io/api/0/projects/1/groups/"), parseResult.Data.Url);
            Assert.Equal("application/json", parseResult.Data.Headers.First(g => g.Key == "Content-Type").Value);
        }

        [Fact]
        public void ParseSettings_UrlWithoutHttp_UrlParsed()
        {
            var service = new CommandLineParser();

            var parseResult = service.Parse(new Span<char>(@"curl sentry.io".ToCharArray()));

            Assert.Equal(HttpMethod.Get, parseResult.Data.HttpMethod);
            Assert.Equal(new Uri("http://sentry.io"), parseResult.Data.Url);
        }

        [Fact]
        public void ParseSettings_UnknownParameterWithUrlAtEnd_UrlParsed()
        {
            var service = new CommandLineParser();

            var parseResult = service.Parse(new Span<char>(@"curl -unknown ""demo"" -X POST -d @file1.txt -d @file2.txt https://example.com/upload".ToCharArray()));

            Assert.Equal(new Uri("https://example.com/upload"), parseResult.Data.Url);
        }

        [Fact]
        public void ParseSettings_UnknownParameterWithUrlAtEnd2_UrlParsed()
        {
            var service = new CommandLineParser();

            var parseResult = service.Parse(new Span<char>(@"curl --user username:password -X POST -d ""browser=Win7x64-C1|Chrome32|1024x768&url=http://www.google.com"" http://crossbrowsertesting.com/api/v3/livetests/".ToCharArray()));

            Assert.Equal(new Uri("http://crossbrowsertesting.com/api/v3/livetests/"), parseResult.Data.Url);
        }
    }
}
