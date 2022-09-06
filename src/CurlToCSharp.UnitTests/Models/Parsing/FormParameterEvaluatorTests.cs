using Curl.Parser.Net.Enums;
using Curl.Parser.Net.Models;
using Curl.Parser.Net.Models.Parsing;

namespace CurlToCSharp.UnitTests.Models.Parsing;

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

    [Fact]
    public void Evaluate_EmptyValue_Success()
    {
        var evaluator = new FormParameterEvaluator();
        var span = new Span<char>("empty=".ToCharArray());
        var convertResult = new ConvertResult<CurlOptions> { Data = new CurlOptions() };

        evaluator.Evaluate(ref span, convertResult);

        Assert.Equal("empty", convertResult.Data.FormData.First().Name);
        Assert.Equal(string.Empty, convertResult.Data.FormData.First().Content);
        Assert.Equal(UploadDataType.Inline, convertResult.Data.FormData.First().Type);
    }

    [Fact]
    public void Evaluate_BinaryFileWithTypeAndFileName_Success()
    {
        var evaluator = new FormParameterEvaluator();
        var span = new Span<char>("web=@index.html;type=text/html;filename=\"test.html\"".ToCharArray());
        var convertResult = new ConvertResult<CurlOptions> { Data = new CurlOptions() };

        evaluator.Evaluate(ref span, convertResult);

        Assert.Equal("web", convertResult.Data.FormData.First().Name);
        Assert.Equal("index.html", convertResult.Data.FormData.First().Content);
        Assert.Equal("text/html", convertResult.Data.FormData.First().ContentType);
        Assert.Equal("test.html", convertResult.Data.FormData.First().FileName);
        Assert.Equal(UploadDataType.BinaryFile, convertResult.Data.FormData.First().Type);
    }

    [Fact]
    public void Evaluate_InvalidValue_Success()
    {
        var evaluator = new FormParameterEvaluator();
        var span = new Span<char>("key=\"\\\"".ToCharArray());
        var convertResult = new ConvertResult<CurlOptions> { Data = new CurlOptions() };

        evaluator.Evaluate(ref span, convertResult);

        Assert.Equal("key", convertResult.Data.FormData.First().Name);
        Assert.Equal("\"", convertResult.Data.FormData.First().Content);
    }
}
