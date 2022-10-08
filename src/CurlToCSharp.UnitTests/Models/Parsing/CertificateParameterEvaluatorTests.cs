using Curl.CommandLine.Parser.Models;
using Curl.CommandLine.Parser.Models.Parsing;

namespace CurlToCSharp.UnitTests.Models.Parsing;

public class CertificateParameterEvaluatorTests
{
    [Fact]
    public void Evaluate_CertificateWithPassword_Success()
    {
        var evaluator = new CertificateParameterEvaluator();
        var span = new Span<char>(@"D:\\cert.p12:123".ToCharArray());
        var convertResult = new ConvertResult<CurlOptions> { Data = new CurlOptions() };

        evaluator.Evaluate(ref span, convertResult);

        Assert.Equal("D:\\cert.p12", convertResult.Data.CertificateFileName);
        Assert.Equal("123", convertResult.Data.CertificatePassword);
    }

    [Fact]
    public void Evaluate_CertificateWithoutPassword_Success()
    {
        var evaluator = new CertificateParameterEvaluator();
        var span = new Span<char>(@"D:\\cert.p12".ToCharArray());
        var convertResult = new ConvertResult<CurlOptions> { Data = new CurlOptions() };

        evaluator.Evaluate(ref span, convertResult);

        Assert.Equal("D:\\cert.p12", convertResult.Data.CertificateFileName);
        Assert.Null(convertResult.Data.CertificatePassword);
    }
}
