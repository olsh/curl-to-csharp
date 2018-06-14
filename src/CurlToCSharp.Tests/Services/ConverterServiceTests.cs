using System;
using System.Collections.Generic;

using CurlToCSharp.Models;
using CurlToCSharp.Services;

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

using Xunit;

namespace CurlToCSharp.Tests.Services
{
    public class ConverterServiceTests
    {
        [Fact]
        public void ToCsharp_ValidCurlOptions_CanBeParsed()
        {
            var converterService = new ConverterService();
            var curlOptions = new CurlOptions
                                  {
                                      HttpMethod = HttpMethod.Post.ToString().ToUpper(),
                                      Url = new Uri("https://google.com"),
                                      PayloadCollection = { "{\"status\": \"resolved\"}" },
                                      UserPasswordPair = "user:pass"
                                  };
            curlOptions.Headers.TryAdd(HeaderNames.ContentType, new StringValues("application/json"));
            curlOptions.Headers.TryAdd(HeaderNames.Authorization, new StringValues("Bearer b7d03a6947b217efb6f3ec3bd3504582"));

            var result = converterService.ToCsharp(curlOptions);

            CSharpSyntaxTree.ParseText(result.Data);
        }
    }
}
