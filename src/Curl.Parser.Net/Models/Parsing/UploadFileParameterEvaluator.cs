using System.Text.RegularExpressions;

using Curl.Parser.Net.Extensions;

namespace Curl.Parser.Net.Models.Parsing;

public class UploadFileParameterEvaluator : ParameterEvaluator
{
    private readonly ParsingOptions _parsingOptions;

    public UploadFileParameterEvaluator(ParsingOptions parsingOptions)
    {
        _parsingOptions = parsingOptions;
        Keys = new HashSet<string> { "-T", "--upload-file" };
    }

    protected override HashSet<string> Keys { get; }

    protected override void EvaluateInner(ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        void AddFilesLimitWarning()
        {
            convertResult.Warnings.Add($"Only first {_parsingOptions.MaxUploadFiles} files were parsed");
        }

        var value = commandLine.ReadValue();

        if (value.IsEmpty)
        {
            return;
        }

        // Comma separated list of files
        if (value.Length > 1 && value[0] == '{' && value[value.Length - 1] == '}')
        {
            var filesSpan = value.Slice(1, value.Length - 2);
            if (filesSpan.IsEmpty)
            {
                return;
            }

            var files = filesSpan.ToString()
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var file in files.Take(_parsingOptions.MaxUploadFiles))
            {
                convertResult.Data.UploadFiles.Add(file.Trim());
            }

            if (files.Length > _parsingOptions.MaxUploadFiles)
            {
                AddFilesLimitWarning();
            }
        }
        else
        {
            // Range of files
            var stringValue = value.ToString();
            var match = Regex.Match(stringValue, @"\[(?<start>\d+)-(?<end>\d+)\]");
            if (match.Success)
            {
                int.TryParse(match.Groups["start"].Value, out var start);
                int.TryParse(match.Groups["end"].Value, out var end);

                if (start >= end)
                {
                    convertResult.Warnings.Add("Invalid upload files range");

                    return;
                }

                var firstPart = stringValue.Substring(0, match.Index);
                var lastPart = stringValue.Substring(match.Index + match.Length);

                var totalFiles = end - start + 1;
                if (totalFiles > _parsingOptions.MaxUploadFiles)
                {
                    AddFilesLimitWarning();
                    end = start + _parsingOptions.MaxUploadFiles - 1;
                }

                for (var i = start; i <= end; i++)
                {
                    convertResult.Data.UploadFiles.Add($"{firstPart}{i}{lastPart}");
                }
            }
            else
            {
                convertResult.Data.UploadFiles.Add(stringValue);
            }
        }
    }
}
