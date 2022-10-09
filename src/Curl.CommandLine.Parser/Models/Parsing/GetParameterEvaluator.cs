using System;
using System.Collections.Generic;

namespace Curl.CommandLine.Parser.Models.Parsing
{
    internal class GetParameterEvaluator : ParameterEvaluator
    {
        public GetParameterEvaluator()
        {
            Keys = new HashSet<string> { "-G", "--get" };
        }

        protected override bool CanBeEmpty => true;

        protected override HashSet<string> Keys { get; }

        protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            convertResult.Data.ForceGet = true;
        }
    }
}
