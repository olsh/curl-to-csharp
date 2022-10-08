using Curl.CommandLine.Parser.Models;

namespace Curl.HttpClient.Converter;

public interface ICurlConverter
{
    ConvertResult<string> ToCsharp(CurlOptions curlOptions);
}
