using System;
using System.Collections.Generic;

using CurlToCSharp.Extensions;

namespace CurlToCSharp.Models.Parsing
{
    public class CertificateTypeParameterEvaluator : ParameterEvaluator
    {
        public CertificateTypeParameterEvaluator()
        {
            Keys = new HashSet<string> { "--cert-type" };
        }

        protected override HashSet<string> Keys { get; }

        protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            var certificateTypeString = commandLine.ReadValue().ToString();
            if (Enum.TryParse(certificateTypeString, true, out CertificateType certificateType))
            {
                convertResult.Data.CertificateType = certificateType;
            }
            else
            {
                convertResult.Warnings.Add($"Unable to parse certificate type {certificateTypeString}, PEM type will be used");
                convertResult.Data.CertificateType = CertificateType.Pem;
            }
        }
    }
}
