using System;
using System.Collections.Generic;

using CurlToCSharp.Extensions;

namespace CurlToCSharp.Models.Parsing
{
    public class DataUrlEncodeParameterEvaluator : DataGenericParameterEvaluator
    {
        public DataUrlEncodeParameterEvaluator()
        {
            Keys = new HashSet<string> { "--data-urlencode" };
        }

        protected override HashSet<string> Keys { get; }

        protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            void AddKeyValue(Span<char> span, int splitIndex, DataContentType contentType)
            {
                var dataKey = span.Slice(0, splitIndex)
                    .ToString();
                var dataValue = span.Slice(splitIndex + 1)
                    .ToString();
                convertResult.Data.Data.Add(new UploadData(dataKey, dataValue, contentType, true));
            }

            var value = commandLine.ReadValue();
            if (value.IsEmpty)
            {
                return;
            }

            var formSeparatorChar = '=';
            var indexOfForm = value.IndexOf(formSeparatorChar);
            if (indexOfForm != -1)
            {
                AddKeyValue(value, indexOfForm, DataContentType.Inline);

                return;
            }

            var indexOfFile = value.IndexOf(FileSeparatorChar);
            if (indexOfFile != -1)
            {
                AddKeyValue(value, indexOfFile, DataContentType.BinaryFile);

                return;
            }

            convertResult.Data.Data.Add(new UploadData(value.ToString(), true));
        }
    }
}
