using Curl.Parser.Net.Models;

namespace Curl.Parser.Net;

public interface ICurlParser
{
    ConvertResult<CurlOptions> Parse(Span<char> commandLine);
}
