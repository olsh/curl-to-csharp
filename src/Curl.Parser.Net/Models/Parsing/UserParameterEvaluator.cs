using Curl.Parser.Net.Extensions;

namespace Curl.Parser.Net.Models.Parsing;

public class UserParameterEvaluator : ParameterEvaluator
{
    public UserParameterEvaluator()
    {
        Keys = new HashSet<string> { "-u", "--user" };
    }

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        convertResult.Data.UserPasswordPair = commandLine.ReadValue()
            .ToString();
    }
}
