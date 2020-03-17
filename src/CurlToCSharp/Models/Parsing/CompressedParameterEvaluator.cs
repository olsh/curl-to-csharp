using System;
using System.Collections.Generic;

namespace CurlToCSharp.Models.Parsing
{
    public class CompressedParameterEvaluator : ParameterEvaluator
    {
        public CompressedParameterEvaluator()
        {
            Keys = new HashSet<string> { "--compressed" };
        }

        protected override bool CanBeEmpty => true;

        protected override HashSet<string> Keys { get; }

        protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            convertResult.Data.IsCompressed = true;
        }
    }
}
