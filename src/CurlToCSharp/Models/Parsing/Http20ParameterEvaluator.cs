namespace CurlToCSharp.Models.Parsing;

public class Http20ParameterEvaluator : ParameterEvaluator
{
    public Http20ParameterEvaluator()
    {
        Keys = new HashSet<string> { "--http2" };
    }

    protected override bool CanBeEmpty => true;

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        convertResult.Data.HttpVersion = HttpVersion.Http20;
    }
}
