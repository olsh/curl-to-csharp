using Curl.Parser.Net.Enums;

namespace Curl.Parser.Net.Models.Parsing;

internal class Http11ParameterEvaluator : ParameterEvaluator
{
    public Http11ParameterEvaluator()
    {
        Keys = new HashSet<string> { "--http1.1" };
    }

    protected override bool CanBeEmpty => true;

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        convertResult.Data.HttpVersion = HttpVersion.Http11;
    }
}
