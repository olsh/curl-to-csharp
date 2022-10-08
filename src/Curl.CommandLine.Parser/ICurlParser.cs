using Curl.CommandLine.Parser.Models;

namespace Curl.CommandLine.Parser;

public interface ICurlParser
{
    ConvertResult<CurlOptions> Parse(Span<char> commandLine);
}
