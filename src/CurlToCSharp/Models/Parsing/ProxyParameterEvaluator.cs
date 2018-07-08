using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using CurlToCSharp.Extensions;

namespace CurlToCSharp.Models.Parsing
{
    public class ProxyParameterEvaluator : ParameterEvaluator
    {
        private static readonly Regex PortRegex = new Regex(@":\d+$", RegexOptions.Compiled);

        public ProxyParameterEvaluator()
        {
            Keys = new HashSet<string> { "-x", "--proxy" };
        }

        protected override HashSet<string> Keys { get; }

        protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            var value = commandLine.ReadValue();
            if (!Uri.TryCreate(value.ToString(), UriKind.Absolute, out Uri proxyUri))
            {
                convertResult.Warnings.Add("Unable to parse proxy URI");

                return;
            }

            // If the port number is not specified in the proxy string, it is assumed to be 1080.
            if (!PortRegex.IsMatch(proxyUri.OriginalString))
            {
                proxyUri = new UriBuilder(proxyUri.Scheme, proxyUri.Host, 1080).Uri;
            }

            convertResult.Data.ProxyUri = proxyUri;
        }
    }
}
