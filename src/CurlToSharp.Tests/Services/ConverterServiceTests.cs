using System;
using System.Collections.Generic;

using CurlToSharp.Models;
using CurlToSharp.Services;

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

using Xunit;

namespace CurlToSharp.Tests.Services
{
    public class ConverterServiceTests
    {
        [Fact]
        public void ToCsharp_ValidCurlOptions_CanBeParsed()
        {
            var converterService = new ConverterService();
            var curlOptions = new CurlOptions
                                  {
                                      HttpMethod = HttpMethod.Post,
                                      Url = new Uri("https://google.com"),
                                      Payload = "{\"status\": \"resolved\"}"
                                  };
            curlOptions.Headers.TryAdd(HeaderNames.ContentType, new StringValues("application/json"));
            curlOptions.Headers.TryAdd(HeaderNames.Authorization, new StringValues("Bearer b7d03a6947b217efb6f3ec3bd3504582"));

            var csharp = converterService.ToCsharp(curlOptions);

            CSharpSyntaxTree.ParseText(csharp);
        }
    }
}
