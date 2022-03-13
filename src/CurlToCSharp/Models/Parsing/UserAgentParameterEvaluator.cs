using CurlToCSharp.Extensions;

using Microsoft.Net.Http.Headers;

namespace CurlToCSharp.Models.Parsing;

public class UserAgentParameterEvaluator : ParameterEvaluator
{
    public UserAgentParameterEvaluator()
    {
        Keys = new HashSet<string> { "-A", "--user-agent" };
    }

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        var value = commandLine.ReadValue();

        convertResult.Data.SetHeader(HeaderNames.UserAgent, value.ToString());
    }
}
