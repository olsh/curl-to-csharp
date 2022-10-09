using System;
using System.Collections.Generic;

using Curl.CommandLine.Parser.Constants;
using Curl.CommandLine.Parser.Extensions;

namespace Curl.CommandLine.Parser.Models.Parsing
{
    internal class UserAgentParameterEvaluator : ParameterEvaluator
    {
        public UserAgentParameterEvaluator()
        {
            Keys = new HashSet<string> { "-A", "--user-agent" };
        }

        protected override HashSet<string> Keys { get; }

        protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            var value = commandLine.ReadValue();

            convertResult.Data.SetHeader(HeaderNames.UserAgent, value.ToString());
        }
    }
}
