using Curl.CommandLine.Parser;
using Curl.CommandLine.Parser.Models.Parsing;

namespace CurlToCSharp.UnitTests.Models.Parsing;

public class ProxyParameterEvaluatorTests
{
    [Fact]
    public void Evaluate_IpWithoutProtocol_UseHttpProtocol()
    {
        var evaluator = new ProxyParameterEvaluator();
        var span = new Span<char>("123.0.4.3:1234".ToCharArray());
        var convertResult = new ConvertResult<CurlOptions> { Data = new CurlOptions() };

        evaluator.Evaluate(ref span, convertResult);

        Assert.Equal(new Uri("http://123.0.4.3:1234"), convertResult.Data.ProxyUri);
    }

    [Fact]
    public void Evaluate_Localhost_UseHttpProtocol()
    {
        var evaluator = new ProxyParameterEvaluator();
        var span = new Span<char>("localhost:1234".ToCharArray());
        var convertResult = new ConvertResult<CurlOptions> { Data = new CurlOptions() };

        evaluator.Evaluate(ref span, convertResult);

        Assert.Equal(new Uri("http://localhost:1234"), convertResult.Data.ProxyUri);
    }
}
