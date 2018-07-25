using System;
using System.Collections.Generic;
using System.Linq;

using CurlToCSharp.Models;
using CurlToCSharp.Services;

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

using Xunit;

namespace CurlToCSharp.UnitTests.Services
{
    public class ConverterServiceTests
    {
        [Fact]
        public void ToCsharp_ValidCurlOptions_CanBeCompiled()
        {
            var converterService = new ConverterService();
            var curlOptions = new CurlOptions
                                  {
                                      HttpMethod = HttpMethod.Post.ToString().ToUpper(),
                                      Url = new Uri("https://google.com"),
                                      UploadData = { new UploadData("{\"status\": \"resolved\"}") },
                                      UserPasswordPair = "user:pass"
                                  };
            curlOptions.Headers.TryAdd(HeaderNames.ContentType, new StringValues("application/json"));
            curlOptions.Headers.TryAdd(HeaderNames.Authorization, new StringValues("Bearer b7d03a6947b217efb6f3ec3bd3504582"));

            var result = converterService.ToCsharp(curlOptions);

            var tree = WrapToClass(result.Data);
            var diagnostics = tree.GetDiagnostics();

            Assert.Empty(diagnostics);
        }

        [Fact]
        public void ToCsharp_GetRequest_ContainsSendStatement()
        {
            var converterService = new ConverterService();
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

        [Fact]
        public void ToCsharp_ContentTypeWithEncoding_StringContentProperlyInitialized()
        {
            var converterService = new ConverterService();
            var curlOptions = new CurlOptions
                                  {
                                      HttpMethod = HttpMethod.Get.ToString()
                                          .ToUpper(),
                                      Url = new Uri("https://google.com"),
                                      UploadData = { new UploadData("content") }
                                  };
            curlOptions.HttpMethod = HttpMethod.Post.ToString()
                .ToUpper();
            curlOptions.Headers.TryAdd(HeaderNames.ContentType, new StringValues("application/json ; charset=utf-8"));

            var result = converterService.ToCsharp(curlOptions);

            var tree = CSharpSyntaxTree.ParseText(result.Data);
            var stringContentConstructor = tree
                .GetRoot()
                .DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .First(oc => oc.Identifier.ValueText == "StringContent");

            Assert.Equal("\"application/json\"", stringContentConstructor.ParameterList.Parameters[2].Identifier.TrailingTrivia.ToString());
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
}
