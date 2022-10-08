using Curl.CommandLine.Parser.Extensions;

namespace Curl.CommandLine.Parser.Models.Parsing;

internal class HeaderParameterEvaluator : ParameterEvaluator
{
    public HeaderParameterEvaluator()
    {
        Keys = new HashSet<string> { "-H", "--header" };
    }

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        var value = commandLine.ReadValue();

        var separatorIndex = value.IndexOf(':');
        if (separatorIndex == -1)
        {
            convertResult.Warnings.Add($"Unable to parse header \"{value.ToString()}\"");

            return;
        }

        var headerKey = value.Slice(0, separatorIndex)
            .ToString()
            .Trim();

        if (string.IsNullOrEmpty(headerKey))
        {
            convertResult.Warnings.Add($"Unable to parse header \"{value.ToString()}\"");

            return;
        }

        var headerValue = string.Empty;
        var valueStartIndex = separatorIndex + 1;
        if (value.Length > valueStartIndex)
        {
            headerValue = value.Slice(valueStartIndex)
                .ToString()
                .Trim();
        }

        if (string.Equals(headerKey, "Cookie"))
        {
            convertResult.Data.CookieValue = headerValue;

            return;
        }

        convertResult.Data.SetHeader(headerKey, headerValue);
    }
}
