using Curl.CommandLine.Parser.Extensions;

namespace Curl.CommandLine.Parser.Models.Parsing;

internal class ProxyCredentialsParameterEvaluator : ParameterEvaluator
{
    public ProxyCredentialsParameterEvaluator()
    {
        Keys = new HashSet<string> { "-U", "--proxy-user" };
    }

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        var value = commandLine.ReadValue();
        if (value.IsEmpty || value.IndexOf(':') == -1)
        {
            convertResult.Warnings.Add("Unable to parse proxy credentials");

            return;
        }

        if (value.Length == 1)
        {
            convertResult.Data.UseDefaultProxyCredentials = true;

            return;
        }

        var passwordSeparatorIndex = value.IndexOf(':');
        convertResult.Data.ProxyUserName = value.Slice(0, passwordSeparatorIndex).ToString();
        convertResult.Data.ProxyPassword = value.Slice(passwordSeparatorIndex + 1).ToString();
    }
}
