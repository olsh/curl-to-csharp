using Curl.Parser.Net.Enums;
using Curl.Parser.Net.Extensions;

namespace Curl.Parser.Net.Models.Parsing;

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
