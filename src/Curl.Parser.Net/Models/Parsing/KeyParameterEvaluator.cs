using Curl.Parser.Net.Extensions;

namespace Curl.Parser.Net.Models.Parsing;

internal class KeyParameterEvaluator : ParameterEvaluator
{
    public KeyParameterEvaluator()
    {
        Keys = new HashSet<string> { "--key" };
    }

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        var value = commandLine.ReadValue();
        if (value.IsEmpty)
        {
            convertResult.Warnings.Add("Unable to parse key");

            return;
        }

        convertResult.Data.KeyFileName = value.ToString();
    }
}
