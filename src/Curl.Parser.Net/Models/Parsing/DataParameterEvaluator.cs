namespace Curl.Parser.Net.Models.Parsing;

internal class DataParameterEvaluator : DataGenericParameterEvaluator
{
    public DataParameterEvaluator()
    {
        Keys = new HashSet<string> { "-d", "--data" };
    }

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        Evaluate(ref commandLine, convertResult, true, false);
    }
}
