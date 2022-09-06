using Curl.Parser.Net.Enums;

namespace Curl.Parser.Net.Models.Parsing;

public class Http10ParameterEvaluator : ParameterEvaluator
{
    public Http10ParameterEvaluator()
    {
        Keys = new HashSet<string> { "-0", "--http1.0" };
    }

    protected override bool CanBeEmpty => true;

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        convertResult.Data.HttpVersion = HttpVersion.Http10;
    }
}
