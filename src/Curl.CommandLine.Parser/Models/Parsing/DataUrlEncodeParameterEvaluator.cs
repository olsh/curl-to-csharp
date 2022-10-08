using Curl.CommandLine.Parser.Enums;
using Curl.CommandLine.Parser.Extensions;

namespace Curl.CommandLine.Parser.Models.Parsing;

internal class DataUrlEncodeParameterEvaluator : ParameterEvaluator
{
    public DataUrlEncodeParameterEvaluator()
    {
        Keys = new HashSet<string> { "--data-urlencode" };
    }

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        void AddKeyValue(Span<char> span, int splitIndex, UploadDataType contentType)
        {
            var dataKey = span.Slice(0, splitIndex)
                .ToString();
            var dataValue = span.Slice(splitIndex + 1)
                .ToString();
            convertResult.Data.UploadData.Add(new UploadData(dataKey, dataValue, contentType, true));
        }

        var value = commandLine.ReadValue();

        var indexOfForm = value.IndexOf(FormSeparatorChar);
        if (indexOfForm != -1)
        {
            AddKeyValue(value, indexOfForm, UploadDataType.Inline);

            return;
        }

        var indexOfFile = value.IndexOf(FileSeparatorChar);
        if (indexOfFile != -1)
        {
            AddKeyValue(value, indexOfFile, UploadDataType.BinaryFile);

            return;
        }

        convertResult.Data.UploadData.Add(new UploadData(value.ToString(), true));
    }
}
