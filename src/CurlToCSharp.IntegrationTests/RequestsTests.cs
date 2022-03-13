using System.Diagnostics;

using CurlToCSharp.IntegrationTests.Constants;
using CurlToCSharp.Models.Parsing;
using CurlToCSharp.Services;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace CurlToCSharp.IntegrationTests;

public class RequestsTests : IClassFixture<EchoWebHostFixture>
{
    [Theory]
    [InlineData("-d \"some data\"")]
    [InlineData("-d key=\"1\"")]
    [InlineData("-d key=\"value with space\"")]
    [InlineData("-d first=\"1=\" -d key=\"broken quotes\\\" -d another=\"")]
    [InlineData("-d \"form=a b\" -d \"another data\"")]
    [InlineData("-d \"some data\" -d @\"Resources/text-file.txt\" -d \"a b\"")]
    [InlineData("--data-binary @\"Resources/text-file.txt\"")]
    [InlineData("--data-urlencode \"a=b c\"")]
    [InlineData("--data-urlencode \"a@Resources/text-file.txt\"")]
    public async Task Data(string arguments)
    {
        await AssertResponsesEqualsAsync(arguments);
    }

    [Theory]
    [InlineData("-d \"some\" -G")]
    [InlineData("-d \"form=a\" -d \"another\" -G")]
    public async Task DataGet(string arguments)
    {
        await AssertResponsesEqualsAsync(arguments);
    }

    [Theory]
    [InlineData("-T \"Resources/text-file.txt\"")]
    public async Task UploadFile(string arguments)
    {
        await AssertResponsesEqualsAsync(arguments);
    }

    [Theory]
    [InlineData("")]
    public async Task Get(string arguments)
    {
        await AssertResponsesEqualsAsync(arguments);
    }

    [Theory]
    [InlineData("-A \"Mozilla/5.0 (iPad; U; CPU OS 3_2_1 like Mac OS X; en-us) AppleWebKit/531.21.10 (KHTML, like Gecko) Mobile/7B405\"")]
    public async Task UserAgentHeader(string arguments)
    {
        await AssertResponsesEqualsAsync(arguments);
    }

    [Theory]
    [InlineData("-H \"content-type: application/json\" -d \"some\"")]
    public async Task ContentTypeLowerCaseHeader(string arguments)
    {
        await AssertResponsesEqualsAsync(arguments);
    }

    [Theory]
    [InlineData("-H \"content-type: application/x-www-form-urlencoded; v=2.0\" -d \"some\"")]
    public async Task ContentTypeWithParametersHeader(string arguments)
    {
        await AssertResponsesEqualsAsync(arguments);
    }

    private static async Task AssertResponsesEqualsAsync(string arguments)
    {
        var curlArguments = $"{new Uri(new Uri(WebHostConstants.TestServerHost), "echo")} {arguments}";

        var curlResponse = ExecuteCurlRequestAsync(curlArguments);
        var csharpTask = ExecuteCsharpRequestAsync(curlArguments);

        await Task.WhenAll(csharpTask, curlResponse).ConfigureAwait(false);

        Assert.Equal(await curlResponse.ConfigureAwait(false), await csharpTask.ConfigureAwait(false));
    }

    private static Task<string> ExecuteCurlRequestAsync(string curlArguments)
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

        Debug.Assert(process != null, nameof(process) + " != null");

        return process.StandardOutput.ReadToEndAsync();
    }

    private static async Task<string> ExecuteCsharpRequestAsync(string curlArguments)
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

        var result = await CSharpScript.EvaluateAsync<HttpResponseMessage>(
            csharp.Data.Replace("var response = ", "return "),
            scriptOptions).ConfigureAwait(false);

        return await result.Content.ReadAsStringAsync().ConfigureAwait(false);
    }
}
