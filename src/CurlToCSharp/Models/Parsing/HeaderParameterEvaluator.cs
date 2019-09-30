using System;
using System.Collections.Generic;

using CurlToCSharp.Extensions;

namespace CurlToCSharp.Models.Parsing
{
    public class HeaderParameterEvaluator : ParameterEvaluator
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
                .Trim()
                .ToString();

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
                    .Trim()
                    .ToString();
            }

            if (string.Equals(headerKey, "Cookie"))
            {
                convertResult.Data.CookieValue = headerValue;

                return;
            }

            convertResult.Data.SetHeader(headerKey, headerValue);
        }
    }
}
