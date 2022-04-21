using CurlToCSharp.Constants;

namespace CurlToCSharp.Extensions;

public static class SpanExtensions
{
    public static Span<char> TrimCommandLine(this Span<char> input)
    {
        return input
            .TrimStart(new ReadOnlySpan<char>(new[] { Chars.Escape, Chars.Space }))
            .Trim();
    }

    public static Span<char> UnEscape(this Span<char> input)
    {
        var list = new LinkedList<char>();
        for (var i = 0; i < input.Length; i++)
        {
            if (input[i] == Chars.Escape && (i == 0 || input[i - 1] != Chars.Escape))
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
            var escaped = start > 0 && input[start - 1] == Chars.Escape;
            if (input[start] != quote || escaped)
            {
                break;
            }
        }

        int end;
        for (end = input.Length - 1; end > start; end--)
        {
            var escaped = input[end - 1] == Chars.Escape;
            if (input[end] != quote || escaped)
            {
                break;
            }
        }

        return input.Slice(start, end + 1 - start);
    }

    public static Span<char> ReadValue(this ref Span<char> commandLine)
    {
        commandLine = commandLine.TrimCommandLine();
        if (commandLine.IsEmpty)
        {
            return commandLine;
        }

        int closeIndex = 0;

        var indexOfSpecialChar = commandLine.IndexOfAny(Chars.SingleQuote, Chars.DoubleQuote, Chars.Space);
        bool firstCharIsQuote = false;
        char firstChar = '\0';
        bool trimLastQuote = true;
        if (indexOfSpecialChar != -1 && commandLine[indexOfSpecialChar] != Chars.Space)
        {
            firstCharIsQuote = true;
            firstChar = commandLine[indexOfSpecialChar];
        }

        if (firstCharIsQuote && commandLine.Length > 1)
        {
            var quote = firstChar;
            for (int i = 0; i < commandLine.Length; i++)
            {
                bool isCurlEscape = commandLine[i] == Chars.SingleQuote && commandLine.Length >= i + 4 && commandLine.ToString().Substring(i, 4) == Chars.CurlSingleQuote;

                bool bbb = indexOfSpecialChar != i && commandLine[i] == quote && (i == 0 || commandLine[i - 1] != Chars.Escape);
                if (!isCurlEscape && bbb)
                {
                    closeIndex = i + 1;
                    break;
                }
                if (isCurlEscape)
                    i += 3;
            }

            if (closeIndex == 0)
            {
                closeIndex = commandLine.Length;
                trimLastQuote = false;
            }
        }
        else
        {
            closeIndex = commandLine.IndexOf(Chars.Space);
            if (closeIndex == -1)
            {
                closeIndex = commandLine.Length;
            }
        }

        var value = commandLine.Slice(0, closeIndex);
        if (firstCharIsQuote)
        {
            if (indexOfSpecialChar == 0)
            {
                value = value.TrimQuotes(firstChar);
            }
            else if (indexOfSpecialChar < closeIndex)
            {
                value = UnEscapeKeyValue(value, indexOfSpecialChar, trimLastQuote);
            }
        }

        commandLine = commandLine.Slice(closeIndex);

        return value;
    }

    public static Span<char> ReadParameter(this ref Span<char> commandLine)
    {
        var indexOfSpace = commandLine.IndexOf(Chars.Space);
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

    public static bool TrySplit(this Span<char> input, char separator, out Span<char> key, out Span<char> value)
    {
        var separatorIndex = input.IndexOf(separator);
        if (separatorIndex == -1)
        {
            value = Span<char>.Empty;
            key = Span<char>.Empty;

            return false;
        }

        key = input.Slice(0, separatorIndex);
        value = input.Slice(separatorIndex + 1);

        return true;
    }

    private static Span<char> UnEscapeKeyValue(Span<char> value, int indexOfSpecialChar, bool trimLastQuote)
    {
        if (trimLastQuote)
        {
            value = new Span<char>(
                value.Slice(0, indexOfSpecialChar)
                    .ToArray()
                    .Concat(value.Slice(indexOfSpecialChar + 1)[..^1].ToArray())
                    .ToArray());
        }
        else
        {
            value = new Span<char>(
                value.Slice(0, indexOfSpecialChar)
                    .ToArray()
                    .Concat(value.Slice(indexOfSpecialChar + 1).ToArray())
                    .ToArray());

        }

        return value;
    }

}
