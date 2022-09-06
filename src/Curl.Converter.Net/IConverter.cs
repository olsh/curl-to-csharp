using Curl.Parser.Net.Models;

namespace Curl.Converter.Net;

public interface IConverter
{
    ConvertResult<string> ToCsharp(CurlOptions curlOptions);
}
