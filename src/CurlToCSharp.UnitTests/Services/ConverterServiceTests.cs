using Curl.CommandLine.Parser;
using Curl.CommandLine.Parser.Models;
using Curl.HttpClient.Converter;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using HeaderNames = Curl.CommandLine.Parser.Constants.HeaderNames;

namespace CurlToCSharp.UnitTests.Services;

public class ConverterServiceTests
{
    [Fact]
    public void ToCsharp_ValidCurlOptions_CanBeCompiled()
    {
        var converterService = new CurlConverter();
        var curlOptions = new CurlOptions
        {
            HttpMethod = HttpMethod.Post.ToString().ToUpper(),
            Url = new Uri("https://google.com"),
            UploadData = { new UploadData("{\"status\": \"resolved\"}") },
            UserPasswordPair = "user:pass"
        };
        curlOptions.SetHeader(HeaderNames.ContentType, "application/json");
        curlOptions.SetHeader(HeaderNames.Authorization, "Bearer b7d03a6947b217efb6f3ec3bd3504582");

        var result = converterService.ToCsharp(curlOptions);

        var tree = WrapToClass(result.Data);
        var diagnostics = tree.GetDiagnostics();

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void ToCsharp_GetRequest_ContainsSendStatement()
    {
        var converterService = new CurlConverter();
        var curlOptions = new CurlOptions
        {
            HttpMethod = HttpMethod.Get.ToString().ToUpper(),
            Url = new Uri("https://google.com")
        };

        var result = converterService.ToCsharp(curlOptions);

        var tree = CSharpSyntaxTree.ParseText(result.Data);
        Assert.NotEmpty(tree.GetRoot()
            .DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Where(ae => ae.Name.Identifier.ValueText == "SendAsync"));
    }

    private SyntaxTree WrapToClass(string code)
    {
        var wrapCode = $@"
class TestClass
{{
    public void TestMethod()
    {{
        {code}
    }}
}}
";

        return CSharpSyntaxTree.ParseText(wrapCode);
    }
}
