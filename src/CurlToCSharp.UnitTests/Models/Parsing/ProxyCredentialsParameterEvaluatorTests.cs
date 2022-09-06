using Curl.Parser.Net.Models;
using Curl.Parser.Net.Models.Parsing;

namespace CurlToCSharp.UnitTests.Models.Parsing;

public class ProxyCredentialsParameterEvaluatorTests
{
    [Fact]
    public void Evaluate_OneColon_UseDefaultCredentials()
    {
        var evaluator = new ProxyCredentialsParameterEvaluator();
        var span = new Span<char>(":".ToCharArray());
        var convertResult = new ConvertResult<CurlOptions> { Data = new CurlOptions() };

        evaluator.Evaluate(ref span, convertResult);

        Assert.True(convertResult.Data.UseDefaultProxyCredentials);
    }

    [Fact]
    public void Evaluate_EmptyPassword_ParsedCorrectly()
    {
        var evaluator = new ProxyCredentialsParameterEvaluator();
        var span = new Span<char>("user:".ToCharArray());
        var convertResult = new ConvertResult<CurlOptions> { Data = new CurlOptions() };

        evaluator.Evaluate(ref span, convertResult);

        Assert.False(convertResult.Data.UseDefaultProxyCredentials);
        Assert.Equal("user", convertResult.Data.ProxyUserName);
        Assert.Equal(string.Empty, convertResult.Data.ProxyPassword);
    }

    [Fact]
    public void Evaluate_ValidValue_ParsedCorrectly()
    {
        var evaluator = new ProxyCredentialsParameterEvaluator();
        var span = new Span<char>("user:pass".ToCharArray());
        var convertResult = new ConvertResult<CurlOptions> { Data = new CurlOptions() };

        evaluator.Evaluate(ref span, convertResult);

        Assert.False(convertResult.Data.UseDefaultProxyCredentials);
        Assert.Equal("user", convertResult.Data.ProxyUserName);
        Assert.Equal("pass", convertResult.Data.ProxyPassword);
    }
}
