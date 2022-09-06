using Curl.Parser.Net.Models;

namespace Curl.Parser.Net;

public interface IParser
{
    ConvertResult<CurlOptions> Parse(Span<char> commandLine);
}
