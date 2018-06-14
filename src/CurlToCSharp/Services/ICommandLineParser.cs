using System;

using CurlToCSharp.Models;

namespace CurlToCSharp.Services
{
    public interface ICommandLineParser
    {
        ParseResult<CurlOptions> Parse(Span<char> commandLine);
    }
}
