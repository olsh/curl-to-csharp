using Curl.Parser.Net.Extensions;

namespace Curl.Parser.Net.Models.Parsing;

internal class RequestParameterEvaluator : ParameterEvaluator
{
    public RequestParameterEvaluator()
    {
        Keys = new HashSet<string> { "-X", "--request" };
    }

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        convertResult.Data.HttpMethod = commandLine.ReadValue()
            .ToString();
    }
}
