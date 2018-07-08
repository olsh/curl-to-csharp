using System;
using System.Collections.Generic;

using CurlToCSharp.Extensions;

namespace CurlToCSharp.Models.Parsing
{
    public class UserParameterEvaluator : ParameterEvaluator
    {
        public UserParameterEvaluator()
        {
            Keys = new HashSet<string> { "-u", "--user" };
        }

        protected override HashSet<string> Keys { get; }

        protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            convertResult.Data.UserPasswordPair = commandLine.ReadValue()
                .ToString();
        }
    }
}
