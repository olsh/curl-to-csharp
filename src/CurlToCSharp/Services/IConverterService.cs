using CurlToCSharp.Models;

namespace CurlToCSharp.Services
{
    public interface IConverterService
    {
        string ToCsharp(CurlOptions curlOptions);
    }
}
