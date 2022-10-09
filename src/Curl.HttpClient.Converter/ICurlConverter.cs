using Curl.CommandLine.Parser;

namespace Curl.HttpClient.Converter
{
    public interface ICurlConverter
    {
        ConvertResult<string> ToCsharp(CurlOptions curlOptions);
    }
}
