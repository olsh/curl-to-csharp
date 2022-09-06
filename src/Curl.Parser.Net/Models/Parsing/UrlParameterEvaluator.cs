using Curl.Parser.Net.Extensions;

namespace Curl.Parser.Net.Models.Parsing;

internal class UrlParameterEvaluator : ParameterEvaluator
{
    public UrlParameterEvaluator()
    {
        Keys = new HashSet<string> { "--url" };
    }

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        var value = commandLine.ReadValue();
        var stringValue = value.ToString();
        if (Uri.TryCreate(stringValue, UriKind.Absolute, out var url) || Uri.TryCreate(
                $"http://{stringValue}",
                UriKind.Absolute,
                out url))
        {
            convertResult.Data.Url = url;
        }
        else
        {
            convertResult.Warnings.Add($"Unable to parse URL \"{stringValue}\"");
        }
    }
}
