using System;
using System.Collections.Generic;

namespace Curl.CommandLine.Parser.Models.Parsing
{
    internal class DataRawParameterEvaluator : DataGenericParameterEvaluator
    {
        public DataRawParameterEvaluator()
        {
            Keys = new HashSet<string> { "--data-raw" };
        }

        protected override HashSet<string> Keys { get; }

        protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            Evaluate(ref commandLine, convertResult, false, false);
        }
    }
}
