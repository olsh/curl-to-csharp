using Curl.Parser.Net.Enums;
using Curl.Parser.Net.Extensions;

namespace Curl.Parser.Net.Models.Parsing;

internal class KeyTypeParameterEvaluator : ParameterEvaluator
{
    public KeyTypeParameterEvaluator()
    {
        Keys = new HashSet<string> { "--key-type" };
    }

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        var value = commandLine.ReadValue().ToString();
        if (Enum.TryParse(value, true, out KeyType keyType))
        {
            convertResult.Data.KeyType = keyType;
        }
        else
        {
            convertResult.Warnings.Add($"Unable to parse key type {value}, PEM type will be used");
            convertResult.Data.KeyType = KeyType.Pem;
        }
    }
}
