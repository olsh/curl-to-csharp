using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

using CurlToCSharp.Models;
using CurlToCSharp.Services;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

using Xunit;

namespace CurlToCSharp.IntegrationTests
{
    public class RequestsTests : IDisposable
    {
        private const string TestServerHost = "http://localhost:4653";

        private readonly IWebHost _webHost;

        public RequestsTests()
        {
            _webHost = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseUrls(TestServerHost)
                .Build();

            Task.Run(() => _webHost.Run());
        }

        [Theory]
        [InlineData("-d \"some data\"")]
        [InlineData("-d \"form=a b\" -d \"another data\"")]
        [InlineData("-d \"some data\" -d @\"Resources\\\\text-file.txt\" -d \"a b\"")]
        [InlineData("--data-binary @\"Resources\\\\text-file.txt\"")]
        [InlineData("--data-urlencode \"a=b c\"")]
        [InlineData("--data-urlencode \"a@Resources\\\\text-file.txt\"")]
        public void Data(string arguments)
        {
            AssertResponsesEquals(arguments);
        }

        [Theory]
        [InlineData("-T \"Resources\\\\text-file.txt\"")]
        public void UploadFile(string arguments)
        {
            AssertResponsesEquals(arguments);
        }

        [Theory]
        [InlineData("")]
        public void Get(string arguments)
        {
            AssertResponsesEquals(arguments);
        }

        public void Dispose()
        {
            _webHost?.Dispose();
        }

        private static void AssertResponsesEquals(string arguments)
        {
            var curlArguments = $"{new Uri(new Uri(TestServerHost), "echo")} {arguments}";

            var curlResponse = ExecuteCurlRequest(curlArguments);
            var csharpResponse = ExecuteCsharpRequest(curlArguments);

            Assert.Equal(curlResponse, csharpResponse);
        }

        private static string ExecuteCurlRequest(string curlArguments)
        {
            var process = Process.Start(
                new ProcessStartInfo
                    {
                        FileName = "curl",
                        Arguments = curlArguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    });

            var readToEnd = process.StandardOutput.ReadToEnd();
            return readToEnd;
        }

        private static string ExecuteCsharpRequest(string curlArguments)
        {
            var commandLineParser = new CommandLineParser(new ParsingOptions(int.MaxValue));
            var converterService = new ConverterService();
            var parserResult = commandLineParser.Parse(new Span<char>($"curl {curlArguments}".ToCharArray()));
            var csharp = converterService.ToCsharp(parserResult.Data);

            var scriptOptions = ScriptOptions.Default.AddReferences(typeof(HttpClient).Assembly)
                .WithImports(
                    "System",
                    "System.Net.Http",
                    "System.Net",
                    "System.Text",
                    "System.IO",
                    "System.Collections.Generic",
                    "System.Text.RegularExpressions");

            var result = CSharpScript.EvaluateAsync<HttpResponseMessage>(
                    csharp.Data.Replace("var response = ", "return "),
                    scriptOptions)
                .Result;

            return result.Content.ReadAsStringAsync()
                .Result;
        }
    }
}
