using Curl.Parser.Net.Models;

namespace Curl.Converter.Net;

public interface ICurlConverter
{
    ConvertResult<string> ToCsharp(CurlOptions curlOptions);
}
