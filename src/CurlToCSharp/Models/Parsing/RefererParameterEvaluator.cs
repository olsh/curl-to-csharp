using CurlToCSharp.Extensions;

namespace CurlToCSharp.Models.Parsing;

public class RefererParameterEvaluator : ParameterEvaluator
{
    private const string AutoRefererParameter = ";auto";

    public RefererParameterEvaluator()
    {
        Keys = new HashSet<string> { "-e", "--referer" };
    }

    protected override bool CanBeEmpty => true;

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        var value = commandLine.ReadValue();

        var autoRefererIndex = value.IndexOf(AutoRefererParameter);
        if (autoRefererIndex != -1)
        {
            convertResult.Warnings.Add($"`{AutoRefererParameter}` is not supported");

            // When there is only `;auto` in the parameter
            if (value.Length == autoRefererIndex)
            {
                return;
            }

            value = value.Slice(0, autoRefererIndex);
        }

        convertResult.Data.SetHeader("Referer", value.ToString());
    }
}
