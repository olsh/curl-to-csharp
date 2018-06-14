using System;

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace CurlToSharp.Models
{
    public class CurlOptions
    {
        public CurlOptions()
        {
            Headers = new HttpRequestHeaders();
        }

        public HttpHeaders Headers { get; }

        public HttpMethod? HttpMethod { get; set; }

        public string Payload { get; set; }

        public Uri Url { get; set; }
    }
}
