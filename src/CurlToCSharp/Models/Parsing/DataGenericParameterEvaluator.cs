using System;

using CurlToCSharp.Constants;
using CurlToCSharp.Extensions;

namespace CurlToCSharp.Models.Parsing
{
    public abstract class DataGenericParameterEvaluator : ParameterEvaluator
    {
        protected void Evaluate(
            ref Span<char> commandLine,
            ConvertResult<CurlOptions> convertResult,
            bool parseFiles,
            bool binary)
        {
            var isFileEntry = parseFiles && commandLine[0] == FileSeparatorChar;
            if (isFileEntry)
            {
                commandLine = commandLine.Slice(1);
            }

            var value = commandLine.ReadValue();
            string stringValue;
            var contentType = UploadDataType.Inline;
            if (isFileEntry)
            {
                contentType = binary ? UploadDataType.BinaryFile : UploadDataType.InlineFile;
                stringValue = value.ToString();
            }
            else
            {
                stringValue = NormalizeValue(value);
            }

            convertResult.Data.UploadData.Add(new UploadData(stringValue, contentType));
        }

        private string NormalizeValue(Span<char> rawValue)
        {
            if (!rawValue.TrySplit(FormSeparatorChar, out Span<char> key, out Span<char> value))
            {
                return rawValue.ToString();
            }

            value = value.Trim(Chars.DoubleQuote);

            return $"{key.ToString()}{FormSeparatorChar}{value.ToString()}";
        }
    }
}