using CurlToCSharp.Models;

namespace CurlToCSharp.Services;

public interface ICommandLineParser
{
    ConvertResult<CurlOptions> Parse(Span<char> commandLine);
}
