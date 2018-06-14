using CurlToSharp.Models;

namespace CurlToSharp.Services
{
    public interface IConverterService
    {
        string ToCsharp(CurlOptions curlOptions);
    }
}
