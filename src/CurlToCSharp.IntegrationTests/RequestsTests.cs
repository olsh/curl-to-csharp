using System;
using System.Diagnostics;
using System.Net.Http;

using CurlToCSharp.IntegrationTests.Constants;
using CurlToCSharp.Models.Parsing;
using CurlToCSharp.Services;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

using Xunit;

namespace CurlToCSharp.IntegrationTests
{
    public class RequestsTests : IClassFixture<EchoWebHostFixture>
    {
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
        [InlineData("-d \"some\" -G")]
        [InlineData("-d \"form=a\" -d \"another\" -G")]
        public void DataGet(string arguments)
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

        [Theory]
        [InlineData("-A \"Mozilla/5.0 (iPad; U; CPU OS 3_2_1 like Mac OS X; en-us) AppleWebKit/531.21.10 (KHTML, like Gecko) Mobile/7B405\"")]
        public void UserAgentHeader(string arguments)
        {
            AssertResponsesEquals(arguments);
        }

        private static void AssertResponsesEquals(string arguments)
        {
            var curlArguments = $"{new Uri(new Uri(WebHostConstants.TestServerHost), "echo")} {arguments}";

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
                    "System.Net.Http.Headers",
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
