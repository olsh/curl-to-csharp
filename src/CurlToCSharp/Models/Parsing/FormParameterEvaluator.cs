using System;
using System.Collections.Generic;

using CurlToCSharp.Extensions;

namespace CurlToCSharp.Models.Parsing
{
    public class FormParameterEvaluator : ParameterEvaluator
    {
        public FormParameterEvaluator()
        {
            Keys = new HashSet<string> { "-F", "--form" };
        }

        protected override HashSet<string> Keys { get; }

        protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            var input = commandLine.ReadValue();
            if (!TrySplit(input, FormSeparatorChar, out Span<char> key, out Span<char> value))
            {
                convertResult.Warnings.Add($"Unable to parse form value \"{input.ToString()}\"");

                return;
            }

            var firstCharOfValue = value[0];
            if (firstCharOfValue == '<')
            {
                convertResult.Data.FormData.Add(new FormData(key.ToString(), value.Slice(1).ToString(), UploadDataType.InlineFile));

                return;
            }

            if (firstCharOfValue == '@')
            {
                convertResult.Data.FormData.Add(new FormData(key.ToString(), value.Slice(1).ToString(), UploadDataType.BinaryFile));

                return;
            }

            convertResult.Data.FormData.Add(new FormData(key.ToString(), value.ToString(), UploadDataType.Inline));
        }

        private bool TrySplit(Span<char> input, char separator, out Span<char> key, out Span<char> value)
        {
            var separatorIndex = input.IndexOf(separator);
            if (separatorIndex == -1)
            {
                value = Span<char>.Empty;
                key = Span<char>.Empty;

                return false;
            }

            key = input.Slice(0, separatorIndex);
            value = input.Slice(separatorIndex + 1);

            return true;
        }
    }
}
