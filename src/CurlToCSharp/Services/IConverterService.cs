using CurlToCSharp.Models;

namespace CurlToCSharp.Services
{
    public interface IConverterService
    {
        ConvertResult<string> ToCsharp(CurlOptions curlOptions);
    }
}
