using Curl.Parser.Net.Extensions;

namespace Curl.Parser.Net.Models.Parsing;

public class CertificateParameterEvaluator : ParameterEvaluator
{
    public CertificateParameterEvaluator()
    {
        Keys = new HashSet<string> { "-E", "--cert" };
    }

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        var certificateValue = commandLine.ReadValue();

        if (certificateValue.IsEmpty)
        {
            convertResult.Warnings.Add("Unable to parse certificate");

            return;
        }

        var passwordSeparatorIndex = certificateValue.LastIndexOf(':');

        // Assuming that certificate file name cannot be one symbol, it's probably a drive letter
        if (passwordSeparatorIndex > 1)
        {
            convertResult.Data.CertificateFileName = certificateValue.Slice(0, passwordSeparatorIndex).ToString();
            convertResult.Data.CertificatePassword = certificateValue.Slice(passwordSeparatorIndex + 1).ToString();
        }
        else
        {
            convertResult.Data.CertificateFileName = certificateValue.ToString();
        }
    }
}
