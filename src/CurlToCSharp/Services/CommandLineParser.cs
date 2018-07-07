using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using CurlToCSharp.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Net.Http.Headers;

namespace CurlToCSharp.Services
{
    public class CommandLineParser : ICommandLineParser
    {
        private const char DoubleQuote = '"';

        private const char SingleQuote = '\'';

        private const char Space = ' ';

        private const char EscapeChar = '\\';

        private const char FileSeparatorChar = '@';

        private readonly ParsingOptions _parsingOptions;

        public CommandLineParser(ParsingOptions parsingOptions)
        {
            _parsingOptions = parsingOptions;
        }

        public ConvertResult<CurlOptions> Parse(Span<char> commandLine)
        {
            if (commandLine.IsEmpty)
            {
                throw new ArgumentException("The command line is empty.", nameof(commandLine));
            }

            var parseResult = new ConvertResult<CurlOptions>(new CurlOptions());
            var parseState = new ParseState();
            while (!commandLine.IsEmpty)
            {
                Trim(ref commandLine);
                if (commandLine.IsEmpty)
                {
                    break;
                }

                if (IsParameter(commandLine))
                {
                    var parameter = ReadParameter(ref commandLine);
                    EvaluateParameter(parameter, ref commandLine, parseResult);
                }
                else
                {
                    var value = ReadValue(ref commandLine);
                    EvaluateValue(parseResult, parseState, value);
                }
            }

            PostParsing(parseResult, parseState);

            return parseResult;
        }

        private static void EvaluateValue(ConvertResult<CurlOptions> convertResult, ParseState parseState, Span<char> value)
        {
            var valueString = value.ToString();
            if (string.Equals(valueString, "curl", StringComparison.InvariantCultureIgnoreCase))
            {
                parseState.IsCurlCommand = true;
            }
            else if (convertResult.Data.Url == null && Uri.TryCreate(valueString, UriKind.Absolute, out var url)
                                                  && !string.IsNullOrEmpty(url.Host))
            {
                convertResult.Data.Url = url;
            }
            else
            {
                parseState.LastUnknownValue = valueString;
            }
        }

        private void EvaluateParameter(Span<char> parameter, ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
        {
            string ReadValueAsString(ref Span<char> span)
            {
                var value = ReadValue(ref span);

                return value.ToString();
            }

            var par = parameter.ToString();

            Trim(ref commandLine);
            if (commandLine.IsEmpty)
            {
                return;
            }

            switch (par)
            {
                    case "-X":
                    case "--request":
                        convertResult.Data.HttpMethod = ReadValueAsString(ref commandLine);
                        break;
                    case "-d":
                    case "--data":
                        EvaluateDataParameter(convertResult, ref commandLine, true, false);
                        break;
                    case "--data-binary":
                        EvaluateDataParameter(convertResult, ref commandLine, true, true);
                        break;
                    case "--data-raw":
                        EvaluateDataParameter(convertResult, ref commandLine, false, false);
                        break;
                    case "-H":
                    case "--header":
                        EvaluateHeaderValue(convertResult, ref commandLine);
                        break;
                    case "-u":
                    case "--user":
                        convertResult.Data.UserPasswordPair = ReadValueAsString(ref commandLine);
                        break;
                    case "--url":
                        EvaluateUrlValue(convertResult, ref commandLine);
                        break;
                    case "-b":
                    case "--cookie":
                        convertResult.Data.CookieValue = ReadValueAsString(ref commandLine);
                        break;
                    case "-x":
                    case "--proxy":
                        EvaluateProxyValue(convertResult, ref commandLine);
                        break;
                    case "-T":
                    case "--upload-file":
                        EvaluateUploadFileParameter(convertResult, ref commandLine);
                        break;
                    case "--data-urlencode":
                        EvaluateUrlEncodeFormData(convertResult, ref commandLine);
                        break;
                    default:
                        convertResult.Warnings.Add($"Parameter \"{par}\" is not supported");
                        break;
            }
        }

        private void EvaluateProxyValue(ConvertResult<CurlOptions> convertResult, ref Span<char> commandLine)
        {
            var value = ReadValue(ref commandLine);
            if (!Uri.TryCreate(value.ToString(), UriKind.Absolute, out Uri proxyUri))
            {
                convertResult.Warnings.Add("Unable to parse proxy URI");

                return;
            }

            // If the port number is not specified in the proxy string, it is assumed to be 1080.
            if (!Regex.IsMatch(proxyUri.OriginalString, @":\d+$"))
            {
                proxyUri = new UriBuilder(proxyUri.Scheme, proxyUri.Host, 1080).Uri;
            }

            convertResult.Data.ProxyUri = proxyUri;
        }

        private void EvaluateUrlValue(ConvertResult<CurlOptions> convertResult, ref Span<char> commandLine)
        {
            var value = ReadValue(ref commandLine);
            var stringValue = value.ToString();
            if (Uri.TryCreate(stringValue, UriKind.Absolute, out var url)
                || Uri.TryCreate($"http://{stringValue}", UriKind.Absolute, out url))
            {
                convertResult.Data.Url = url;
            }
            else
            {
                convertResult.Warnings.Add($"Unable to parse URL \"{stringValue}\"");
            }
        }

        private void EvaluateHeaderValue(ConvertResult<CurlOptions> convertResult, ref Span<char> commandLine)
        {
            var value = ReadValue(ref commandLine);

            var separatorIndex = value.IndexOf(':');
            if (separatorIndex == -1)
            {
                convertResult.Warnings.Add($"Unable to parse header \"{value.ToString()}\"");
                return;
            }

            var headerKey = value.Slice(0, separatorIndex).ToString().Trim();

            string headerValue = string.Empty;
            var valueStartIndex = separatorIndex + 1;
            if (value.Length > valueStartIndex)
            {
                headerValue = value.Slice(valueStartIndex).ToString().Trim();
            }

            if (string.IsNullOrEmpty(headerValue) || !convertResult.Data.Headers.TryAdd(headerKey, headerValue))
            {
                convertResult.Warnings.Add($"Unable to add header \"{headerKey}\": \"{headerValue}\"");
            }
        }

        private void EvaluateDataParameter(
            ConvertResult<CurlOptions> convertResult,
            ref Span<char> commandLine,
            bool parseFiles,
            bool binary)
        {
            var isFileEntry = parseFiles && commandLine[0] == FileSeparatorChar;
            if (isFileEntry)
            {
                commandLine = commandLine.Slice(1);
            }

            var value = ReadValue(ref commandLine);
            var contentType = DataContentType.Inline;
            if (isFileEntry)
            {
                contentType = binary ? DataContentType.BinaryFile : DataContentType.EscapedFile;
            }

            convertResult.Data.Data.Add(new UploadData(value.ToString(), contentType));
        }

        private void EvaluateUrlEncodeFormData(ConvertResult<CurlOptions> convertResult, ref Span<char> commandLine)
        {
            void AddKeyValue(Span<char> span, int splitIndex, DataContentType contentType)
            {
                var dataKey = span.Slice(0, splitIndex)
                    .ToString();
                var dataValue = span.Slice(splitIndex + 1)
                    .ToString();
                convertResult.Data.Data.Add(new UploadData(dataKey, dataValue, contentType, true));
            }

            var value = ReadValue(ref commandLine);
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

        private void EvaluateUploadFileParameter(ConvertResult<CurlOptions> convertResult, ref Span<char> commandLine)
        {
            void AddFilesLimitWarning()
            {
                convertResult.Warnings.Add($"Only first {_parsingOptions.MaxUploadFiles} files were parsed");
            }

            var value = ReadValue(ref commandLine);

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
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
                foreach (var file in files.Take(_parsingOptions.MaxUploadFiles))
                {
                    convertResult.Data.UploadFiles.Add(file.Trim());
                }

                if (files.Count > _parsingOptions.MaxUploadFiles)
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
                    int.TryParse(match.Groups["start"].Value, out int start);
                    int.TryParse(match.Groups["end"].Value, out int end);

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

                    for (int i = start; i <= end; i++)
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

        private Span<char> ReadParameter(ref Span<char> commandLine)
        {
            var indexOfSpace = commandLine.IndexOf(Space);
            if (indexOfSpace == -1)
            {
                indexOfSpace = commandLine.Length;
            }

            var parameter = commandLine.Slice(0, indexOfSpace);
            commandLine = commandLine.Slice(indexOfSpace);

            return parameter;
        }

        private Span<char> ReadValue(ref Span<char> commandLine)
        {
            Trim(ref commandLine);
            if (commandLine.IsEmpty)
            {
                return commandLine;
            }

            var firstChar = commandLine[0];
            int closeIndex = 0;
            var firstCharIsQuote = firstChar == SingleQuote || firstChar == DoubleQuote;
            if (firstCharIsQuote && commandLine.Length > 1)
            {
                var quote = firstChar;
                commandLine = commandLine.Slice(1);
                for (int i = 0; i < commandLine.Length; i++)
                {
                    if (commandLine[i] == quote && (i == 0 || commandLine[i - 1] != EscapeChar))
                    {
                        closeIndex = i + 1;
                        break;
                    }
                }
            }
            else
            {
                closeIndex = commandLine.IndexOf(Space);
                if (closeIndex == -1)
                {
                    closeIndex = commandLine.Length;
                }
            }

            if (closeIndex == -1)
            {
                return Span<char>.Empty;
            }

            var value = commandLine.Slice(0, closeIndex);
            if (firstCharIsQuote)
            {
                TrimQuotes(ref value, firstChar);
            }

            commandLine = commandLine.Slice(closeIndex);

            return UnEscape(value);
        }

        private Span<char> UnEscape(Span<char> input)
        {
            var list = new LinkedList<char>();
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == EscapeChar && (i == 0 || input[i - 1] != EscapeChar))
                {
                    continue;
                }

                list.AddLast(input[i]);
            }

            return new Span<char>(list.ToArray());
        }

        private void Trim(ref Span<char> input)
        {
            int start;
            for (start = 0; start < input.Length; start++)
            {
                if (!char.IsWhiteSpace(input[start]) && input[start] != EscapeChar)
                {
                    break;
                }
            }

            int end;
            for (end = input.Length - 1; end > start; end--)
            {
                if (!char.IsWhiteSpace(input[end]))
                {
                    break;
                }
            }

            input = input.Slice(start, end - start + 1);
        }

        private void TrimQuotes(ref Span<char> input, char quote)
        {
            int start;
            for (start = 0; start < input.Length; start++)
            {
                var escaped = start > 0 && input[start - 1] == EscapeChar;
                if (input[start] != quote || escaped)
                {
                    break;
                }
            }

            int end;
            for (end = input.Length - 1; end > start; end--)
            {
                var escaped = input[end - 1] == EscapeChar;
                if (input[end] != quote || escaped)
                {
                    break;
                }
            }

            input = input.Slice(start, end + 1 - start);
        }

        private bool IsParameter(Span<char> commandLine)
        {
            return commandLine.IndexOf('-') == 0;
        }

        private void PostParsing(ConvertResult<CurlOptions> result, ParseState state)
        {
            if (result.Data.Url == null
                && !string.IsNullOrWhiteSpace(state.LastUnknownValue)
                && Uri.TryCreate($"http://{state.LastUnknownValue}", UriKind.Absolute, out Uri url))
            {
                result.Data.Url = url;
            }

            // This option overrides -F, --form and -I, --head and -T, --upload-file.
            if (result.Data.HasDataPayload)
            {
                result.Data.UploadFiles.Clear();
            }

            var hasFileUpload = result.Data.UploadFiles.Any();
            if (result.Data.HttpMethod == null)
            {
                if (result.Data.HasDataPayload)
                {
                    result.Data.HttpMethod = HttpMethod.Post.ToString()
                        .ToUpper();
                }
                else if (hasFileUpload)
                {
                    result.Data.HttpMethod = HttpMethod.Put.ToString()
                        .ToUpper();
                }
                else
                {
                    result.Data.HttpMethod = HttpMethod.Get.ToString()
                        .ToUpper();
                }
            }

            if (!result.Data.Headers.GetCommaSeparatedValues(HeaderNames.ContentType)
                    .Any() && result.Data.HasDataPayload)
            {
                result.Data.Headers.TryAdd(HeaderNames.ContentType, "application/x-www-form-urlencoded");
            }

            if (!state.IsCurlCommand)
            {
                result.Errors.Add("Invalid curl command");
            }

            if (result.Data.Url == null)
            {
                result.Errors.Add("Unable to parse URL");
            }
        }
    }
}

