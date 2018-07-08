using System;

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
            var contentType = DataContentType.Inline;
            if (isFileEntry)
            {
                contentType = binary ? DataContentType.BinaryFile : DataContentType.EscapedFile;
            }

            convertResult.Data.Data.Add(new UploadData(value.ToString(), contentType));
        }
    }
}