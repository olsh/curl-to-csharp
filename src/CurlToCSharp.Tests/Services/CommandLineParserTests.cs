using System;
using System.Linq;
using System.Net.Http;

using CurlToCSharp.Models;
using CurlToCSharp.Services;

using Microsoft.Net.Http.Headers;

using Xunit;

namespace CurlToCSharp.Tests.Services
{
    public class CommandLineParserTests
    {
        [Fact]
        public void ParseSettings_SimpleConfiguration_DefaultMethodIsGet()
        {
            var service = CreateCommandLineParser();

            var parseResult = service.Parse(new Span<char>(@"curl -i https://sentry.io/api/0/".ToCharArray()));

            Assert.Equal(HttpMethod.Get.ToString().ToUpper(), parseResult.Data.HttpMethod);
            Assert.Equal(new Uri("https://sentry.io/api/0/"), parseResult.Data.Url);
        }

        [Fact]
        public void ParseSettings_ValidConfiguration_Success()
        {
            var service = CreateCommandLineParser();

            var curl = @"curl -X POST -H ""Content-Type: application/json"" -H ""Authorization: Bearer b7d03a6947b217efb6f3ec3bd3504582"" -d '{""type"":""A"",""name"":""www"",""data"":""162.10.66.0"",""priority"":null,""port"":null,""weight"":null}' ""https://api.digitalocean.com/v2/domains/example.com/records""";
            var parseResult = service.Parse(new Span<char>(curl.ToCharArray()));

            Assert.Equal(@"{""type"":""A"",""name"":""www"",""data"":""162.10.66.0"",""priority"":null,""port"":null,""weight"":null}", parseResult.Data.Payload);
            Assert.Equal(HttpMethod.Post.ToString().ToUpper(), parseResult.Data.HttpMethod);
            Assert.Equal(new Uri("https://api.digitalocean.com/v2/domains/example.com/records"), parseResult.Data.Url);
            Assert.Equal("Bearer b7d03a6947b217efb6f3ec3bd3504582", parseResult.Data.Headers.First(g => g.Key == "Authorization").Value);
        }

        [Fact]
        public void ParseSettings_Multiline_Success()
        {
            var service = CreateCommandLineParser();

            var parseResult = service.Parse(new Span<char>(
                @"$ curl -i https://sentry.io/api/0/projects/1/groups/ \
                    -d '{""status"": ""resolved""}' \
                    -H 'Content-Type: application/json'".ToCharArray()));

            Assert.Equal(@"{""status"": ""resolved""}", parseResult.Data.Payload);
            Assert.Equal(HttpMethod.Post.ToString().ToUpper(), parseResult.Data.HttpMethod);
            Assert.Equal(new Uri("https://sentry.io/api/0/projects/1/groups/"), parseResult.Data.Url);
            Assert.Equal("application/json", parseResult.Data.Headers.First(g => g.Key == "Content-Type").Value);
        }

        [Fact]
        public void ParseSettings_UrlWithoutHttp_UrlParsed()
        {
            var service = CreateCommandLineParser();

            var parseResult = service.Parse(new Span<char>(@"curl sentry.io".ToCharArray()));

            Assert.Equal(HttpMethod.Get.ToString().ToUpper(), parseResult.Data.HttpMethod);
            Assert.Equal(new Uri("http://sentry.io"), parseResult.Data.Url);
        }

        [Fact]
        public void ParseSettings_UnknownParameterWithUrlAtEnd_UrlParsed()
        {
            var service = CreateCommandLineParser();

            var curl = @"curl -unknown ""demo"" -X POST -d @file1.txt -d @file2.txt https://example.com/upload";
            var parseResult = service.Parse(new Span<char>(curl.ToCharArray()));

            Assert.Equal(new Uri("https://example.com/upload"), parseResult.Data.Url);
        }

        [Fact]
        public void ParseSettings_UnknownParameterWithUrlAtEnd2_UrlParsed()
        {
            var service = CreateCommandLineParser();

            var curl = @"curl --user username:password -X POST -d ""browser=Win7x64-C1|Chrome32|1024x768&url=http://www.google.com"" http://crossbrowsertesting.com/api/v3/livetests/";
            var parseResult = service.Parse(new Span<char>(curl.ToCharArray()));

            Assert.Equal(new Uri("http://crossbrowsertesting.com/api/v3/livetests/"), parseResult.Data.Url);
            Assert.Equal("username:password", parseResult.Data.UserPasswordPair);
        }

        [Fact]
        public void ParseSettings_MultipleData_DataConcatenated()
        {
            var service = CreateCommandLineParser();

            var curl = @"curl -X POST https://api.easypost.com/v2/shipments \
                             -u API_KEY: \
                             -d '1' \
                             -d '2' \
                             -d '3'";
            var parseResult = service.Parse(new Span<char>(curl.ToCharArray()));

            Assert.Equal("1&2&3", parseResult.Data.Payload);
        }

        [Fact]
        public void ParseSettings_FilesData_FilesParsed()
        {
            var service = CreateCommandLineParser();

            var curl = @"curl -u ""demo"" -X POST -d @""file1.txt"" -d @file2.txt https://example.com/upload";
            var parseResult = service.Parse(new Span<char>(curl.ToCharArray()));

            Assert.Equal(2, parseResult.Data.DataFiles.Count);
            Assert.Equal("file1.txt", parseResult.Data.DataFiles.ElementAt(0));
            Assert.Equal("file2.txt", parseResult.Data.DataFiles.ElementAt(1));
        }

        [Fact]
        public void ParseSettings_EmptyParameter_DoesntFail()
        {
            var service = CreateCommandLineParser();

            var curl = @"curl -u";
            service.Parse(new Span<char>(curl.ToCharArray()));
        }

        [Fact]
        public void ParseSettings_EscapedString_CorrectlyParsed()
        {
            var service = CreateCommandLineParser();

            var curl = @"curl -d ""\""""";
            var parseResult = service.Parse(new Span<char>(curl.ToCharArray()));

            Assert.Equal("\"", parseResult.Data.Payload);
        }

        [Fact]
        public void ParseSettings_MultipleEscapedString_CorrectlyParsed()
        {
            var service = CreateCommandLineParser();

            var curl = @"curl -d ""\"""" -d '\'' -d '""' -d ""'""";
            var parseResult = service.Parse(new Span<char>(curl.ToCharArray()));

            Assert.Equal("\"&\'&\"&\'", parseResult.Data.Payload);
        }

        [Fact]
        public void ParseSettings_HeaderWithMultipleSeparators_CorrectlyParsed()
        {
            var service = CreateCommandLineParser();

            var curl = @"curl http://fiddle.jshell.net/echo/html/ -H 'Referer: http://fiddle.jshell.net/_display/'";
            var parseResult = service.Parse(new Span<char>(curl.ToCharArray()));

            Assert.Equal("http://fiddle.jshell.net/_display/", parseResult.Data.Headers.First(h => h.Key == "Referer").Value);
        }

        [Fact]
        public void ParseSettings_NewLines_CorrectlyParsed()
        {
            var service = CreateCommandLineParser();

            var curl = @"curl \
https://test.zendesk.com/api/v2/tickets.json \
-d '\\some_data' \
-v -u {email_address}:{password} -X \ 
POST";

            var parseResult = service.Parse(new Span<char>(curl.ToCharArray()));

            Assert.Equal(HttpMethod.Post.ToString().ToUpper(), parseResult.Data.HttpMethod);
            Assert.Equal("\\some_data", parseResult.Data.Payload);
            Assert.Equal("https://test.zendesk.com/api/v2/tickets.json", parseResult.Data.Url.ToString());
        }

        [Fact]
        public void ParseSettings_UploadSingleFile_CorrectlyParsed()
        {
            var service = CreateCommandLineParser();

            var curl = @"curl --upload-file 'file.txt' http://www.example.com";

            var parseResult = service.Parse(new Span<char>(curl.ToCharArray()));

            Assert.Equal(HttpMethod.Put.ToString().ToUpper(), parseResult.Data.HttpMethod);
            Assert.Equal(1, parseResult.Data.UploadFiles.Count);
            Assert.Contains("file.txt", parseResult.Data.UploadFiles);
        }

        [Fact]
        public void ParseSettings_UploadCommaSeparatedFiles_CorrectlyParsed()
        {
            var service = CreateCommandLineParser();

            var curl = @"curl --upload-file ""{file1,file2}"" http://www.example.com";

            var parseResult = service.Parse(new Span<char>(curl.ToCharArray()));

            Assert.Equal(2, parseResult.Data.UploadFiles.Count);
            Assert.Contains("file1", parseResult.Data.UploadFiles);
            Assert.Contains("file2", parseResult.Data.UploadFiles);
        }

        [Fact]
        public void ParseSettings_UploadRangeFiles_CorrectlyParsed()
        {
            var service = CreateCommandLineParser();

            var curl = @"curl -T ""img[1-3].png"" http://www.example.com";

            var parseResult = service.Parse(new Span<char>(curl.ToCharArray()));

            Assert.Equal(3, parseResult.Data.UploadFiles.Count);
            Assert.Contains("img1.png", parseResult.Data.UploadFiles);
            Assert.Contains("img2.png", parseResult.Data.UploadFiles);
            Assert.Contains("img3.png", parseResult.Data.UploadFiles);
        }

        [Fact]
        public void ParseSettings_EmptyHeaderValue_DoNotAddHeader()
        {
            var service = CreateCommandLineParser();

            var parseResult = service.Parse(new Span<char>(
                @"$ curl https://sentry.io/api/0/projects/1/groups/ \
                    -d '{""status"": ""resolved""}' \
                    -H 'Content-Length:'".ToCharArray()));

            Assert.Empty(parseResult.Data.Headers.Where(h => h.Key == HeaderNames.ContentLength));
        }

        private static CommandLineParser CreateCommandLineParser()
        {
            return new CommandLineParser(new ParsingOptions(10));
        }
    }
}
