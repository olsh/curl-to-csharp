using System;
using System.Collections.Generic;

using CurlToCSharp.Extensions;

using Microsoft.Net.Http.Headers;

namespace CurlToCSharp.Models.Parsing
{
    public class UserAgentEvaluator : ParameterEvaluator
    {
        public UserAgentEvaluator()
        {
            Keys = new HashSet<string> { "-A", "--user-agent" };
        }

        protected override HashSet<string> Keys { get; }

        protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            var value = commandLine.ReadValue();

            if (!convertResult.Data.Headers.TryAdd(HeaderNames.UserAgent, value.ToString()))
            {
                convertResult.Warnings.Add("Unable to set User-Agent header");
            }
        }
    }
}
