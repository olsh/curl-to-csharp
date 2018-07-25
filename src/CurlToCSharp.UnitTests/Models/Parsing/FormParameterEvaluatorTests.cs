using System;
using System.Linq;

using CurlToCSharp.Models;
using CurlToCSharp.Models.Parsing;

using Xunit;

namespace CurlToCSharp.UnitTests.Models.Parsing
{
    public class FormParameterEvaluatorTests
    {
        [Fact]
        public void Evaluate_BinaryFile_Success()
        {
            var evaluator = new FormParameterEvaluator();
            var span = new Span<char>("web=@index.html".ToCharArray());
            var convertResult = new ConvertResult<CurlOptions> { Data = new CurlOptions() };

            evaluator.Evaluate(ref span, convertResult);

            Assert.Equal("web", convertResult.Data.FormData.First().Name);
            Assert.Equal("index.html", convertResult.Data.FormData.First().Content);
            Assert.Equal(UploadDataType.BinaryFile, convertResult.Data.FormData.First().Type);
        }

        [Fact]
        public void Evaluate_InlineContent_Success()
        {
            var evaluator = new FormParameterEvaluator();
            var span = new Span<char>("name=John".ToCharArray());
            var convertResult = new ConvertResult<CurlOptions> { Data = new CurlOptions() };

            evaluator.Evaluate(ref span, convertResult);

            Assert.Equal("name", convertResult.Data.FormData.First().Name);
            Assert.Equal("John", convertResult.Data.FormData.First().Content);
            Assert.Equal(UploadDataType.Inline, convertResult.Data.FormData.First().Type);
        }

        [Fact]
        public void Evaluate_InlineFile_Success()
        {
            var evaluator = new FormParameterEvaluator();
            var span = new Span<char>("story=<hugefile.txt".ToCharArray());
            var convertResult = new ConvertResult<CurlOptions> { Data = new CurlOptions() };

            evaluator.Evaluate(ref span, convertResult);

            Assert.Equal("story", convertResult.Data.FormData.First().Name);
            Assert.Equal("hugefile.txt", convertResult.Data.FormData.First().Content);
            Assert.Equal(UploadDataType.InlineFile, convertResult.Data.FormData.First().Type);
        }
    }
}
