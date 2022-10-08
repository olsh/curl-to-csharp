namespace Curl.CommandLine.Parser.Models.Parsing;

internal class HeadParameterEvaluator : ParameterEvaluator
{
    public HeadParameterEvaluator()
    {
        Keys = new HashSet<string> { "-I", "--head" };
    }

    protected override bool CanBeEmpty => true;

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        convertResult.Data.HttpMethod = HttpMethod.Head.ToString().ToUpper();
    }
}
