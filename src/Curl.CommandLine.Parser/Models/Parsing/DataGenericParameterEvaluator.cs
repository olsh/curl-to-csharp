using System;

using Curl.CommandLine.Parser.Enums;
using Curl.CommandLine.Parser.Extensions;

namespace Curl.CommandLine.Parser.Models.Parsing
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
            var contentType = UploadDataType.Inline;
            if (isFileEntry)
            {
                contentType = binary ? UploadDataType.BinaryFile : UploadDataType.InlineFile;
            }

            convertResult.Data.UploadData.Add(new UploadData(value.ToString(), contentType));
        }
    }
}
