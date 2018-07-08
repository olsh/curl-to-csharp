using System;
using System.Collections.Generic;
using System.Linq;

namespace CurlToCSharp.Extensions
{
    public static class SpanExtensions
    {
        private const char EscapeChar = '\\';

        private const char DoubleQuote = '"';

        private const char SingleQuote = '\'';

        private const char Space = ' ';

        public static Span<char> Trim(this Span<char> input)
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

            return input.Slice(start, end - start + 1);
        }

        public static Span<char> UnEscape(this Span<char> input)
        {
            var list = new LinkedList<char>();
            for (var i = 0; i < input.Length; i++)
            {
                if (input[i] == EscapeChar && (i == 0 || input[i - 1] != EscapeChar))
                {
                    continue;
                }

                list.AddLast(input[i]);
            }

            return new Span<char>(list.ToArray());
        }

        public static Span<char> TrimQuotes(this Span<char> input, char quote)
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

            return input.Slice(start, end + 1 - start);
        }

        public static Span<char> ReadValue(this ref Span<char> commandLine)
        {
            commandLine = commandLine.Trim();
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
                value = value.TrimQuotes(firstChar);
            }

            commandLine = commandLine.Slice(closeIndex);

            return value.UnEscape();
        }

        public static Span<char> ReadParameter(this ref Span<char> commandLine)
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

        public static bool IsParameter(this Span<char> commandLine)
        {
            return commandLine.IndexOf('-') == 0;
        }
    }
}
