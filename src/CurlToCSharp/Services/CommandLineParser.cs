using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using CurlToCSharp.Models;

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;

namespace CurlToCSharp.Services
{
    public class CommandLineParser : ICommandLineParser
    {
        private const char DoubleQuote = '"';

        private const char SingleQuote = '\'';

        private const char Space = ' ';

        private const char EscapeChar = '\\';

        public ConvertResult<CurlOptions> Parse(Span<char> commandLine)
        {
            if (commandLine.IsEmpty)
            {
                throw new ArgumentException("The command line is empty.", nameof(commandLine));
            }

            Trim(ref commandLine);

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

            if (parseResult.Data.HttpMethod == null)
            {
                parseResult.Data.HttpMethod = HttpMethod.Get.ToString().ToUpper();
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
            string ReadValue(ref Span<char> span)
            {
                var value = this.ReadValue(ref span);

                return value.ToString();
            }

            var par = parameter.ToString();

            if (string.IsNullOrWhiteSpace(par))
            {
                return;
            }

            string val;
            switch (par)
            {
                    case "-X":
                    case "--request":
                        convertResult.Data.HttpMethod = ReadValue(ref commandLine);
                        break;
                    case "-d":
                    case "--data":
                        EvaluateDataParameter(convertResult, ref commandLine);
                        break;
                    case "-H":
                    case "--header":
                        val = ReadValue(ref commandLine);
                        if (!convertResult.Data.Headers.TryAdd(val.Split(":")[0].Trim(),  new StringValues(val.Split(":")[1].Trim())))
                        {
                            // Add error
                        }

                        break;
                    case "-u":
                    case "--user":
                        convertResult.Data.UserPasswordPair = ReadValue(ref commandLine);
                    break;
                    default:
                        convertResult.Warnings.Add($"Parameter \"{par}\" is not supported yet");
                        break;
            }
        }

        private void EvaluateDataParameter(ConvertResult<CurlOptions> convertResult, ref Span<char> commandLine)
        {
            var value = ReadValue(ref commandLine);
            if (!value.IsEmpty)
            {
                if (value[0] == '@')
                {
                    convertResult.Data.Files.Add(value.Slice(1).ToString());
                }
                else
                {
                    convertResult.Data.PayloadCollection.Add(value.ToString());
                }

                if (convertResult.Data.HttpMethod == null)
                {
                    convertResult.Data.HttpMethod = HttpMethod.Post.ToString()
                        .ToUpper();
                }
            }
        }

        private Span<char> ReadParameter(ref Span<char> commandLine)
        {
            Trim(ref commandLine);

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
                if (input[i] == EscapeChar && (i == 0 || input[i - 1] == EscapeChar))
                {
                    continue;
                }

                list.AddLast(input[i]);
            }

            return new Span<char>(list.ToArray());
        }

        private void Trim(ref Span<char> input)
        {
            char space = ' ';
            int start;
            for (start = 0; start < input.Length; start++)
            {
                if (input[start] != space)
                {
                    break;
                }
            }

            int end;
            for (end = input.Length - 1; end > 0; end--)
            {
                if (input[end] != space)
                {
                    break;
                }
            }

            input = input.Slice(start, end + 1 - start);
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
