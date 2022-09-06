using Curl.Parser.Net.Enums;

namespace Curl.Parser.Net.Models.Parsing;

internal class Http30ParameterEvaluator : ParameterEvaluator
{
    public Http30ParameterEvaluator()
    {
        Keys = new HashSet<string> { "--http3" };
    }

    protected override bool CanBeEmpty => true;

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        convertResult.Data.HttpVersion = HttpVersion.Http30;
    }
}
