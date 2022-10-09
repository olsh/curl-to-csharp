using Curl.CommandLine.Parser;
using Curl.CommandLine.Parser.Models.Parsing;

namespace CurlToCSharp.UnitTests.Models.Parsing;

public class HeaderParameterEvaluatorTests
{
    [Fact]
    public void Evaluate_InvalidHeaderWithLeadingSeparator_WarningAdded()
    {
        var evaluator = new HeaderParameterEvaluator();
        var span = new Span<char>(":User-Agent: curl/7.60.0".ToCharArray());
        var convertResult = new ConvertResult<CurlOptions> { Data = new CurlOptions() };

        evaluator.Evaluate(ref span, convertResult);

        Assert.Empty(convertResult.Data.Headers);
        Assert.Equal(1, convertResult.Warnings.Count);
    }

    [Fact]
    public void Evaluate_CookieHeader_CookieAdded()
    {
        var evaluator = new HeaderParameterEvaluator();
        var span = new Span<char>("'Cookie: login=123'".ToCharArray());
        var convertResult = new ConvertResult<CurlOptions> { Data = new CurlOptions() };

        evaluator.Evaluate(ref span, convertResult);

        Assert.Equal("login=123", convertResult.Data.CookieValue);
        Assert.Empty(convertResult.Data.Headers);
    }
}
