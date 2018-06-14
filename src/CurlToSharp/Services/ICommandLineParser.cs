using System;

using CurlToSharp.Models;

namespace CurlToSharp.Services
{
    public interface ICommandLineParser
    {
        ParseResult<CurlOptions> Parse(Span<char> commandLine);
    }
}
