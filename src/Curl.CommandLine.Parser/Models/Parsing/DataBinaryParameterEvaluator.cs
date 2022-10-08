namespace Curl.CommandLine.Parser.Models.Parsing;

internal class DataBinaryParameterEvaluator : DataGenericParameterEvaluator
{
    public DataBinaryParameterEvaluator()
    {
        Keys = new HashSet<string> { "--data-binary" };
    }

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        Evaluate(ref commandLine, convertResult, true, true);
    }
}
