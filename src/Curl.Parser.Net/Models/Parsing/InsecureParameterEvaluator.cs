namespace Curl.Parser.Net.Models.Parsing;

internal class InsecureParameterEvaluator : ParameterEvaluator
{
    public InsecureParameterEvaluator()
    {
        Keys = new HashSet<string> { "-k", "--insecure" };
    }

    protected override bool CanBeEmpty => true;

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        convertResult.Data.Insecure = true;
    }
}
