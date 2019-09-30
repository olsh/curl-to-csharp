using System;
using System.Collections.Generic;

using CurlToCSharp.Constants;
using CurlToCSharp.Extensions;

namespace CurlToCSharp.Models.Parsing
{
    public abstract class ParameterEvaluator
    {
        protected const char FileSeparatorChar = '@';

        protected const char FormSeparatorChar = '=';

        protected abstract HashSet<string> Keys { get; }

        protected virtual bool CanBeEmpty => false;

        public bool CanEvaluate(string handle)
        {
            return Keys.Contains(handle);
        }

        public void Evaluate(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            commandLine = commandLine.TrimStart(Chars.Escape).Trim();
            if (!CanBeEmpty && commandLine.IsEmpty)
            {
                return;
            }

            EvaluateInner(ref commandLine, convertResult);
        }

        protected abstract void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult);
    }
}
