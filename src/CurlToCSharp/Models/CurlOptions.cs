using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace CurlToCSharp.Models
{
    public class CurlOptions
    {
        public CurlOptions()
        {
            Headers = new HttpRequestHeaders();
            PayloadCollection = new List<string>();
            Files = new List<string>();
        }

        public string CookieValue { get; set; }

        public ICollection<string> Files { get; }

        public bool HasCookies => !string.IsNullOrWhiteSpace(CookieValue);

        public bool HasProxy => ProxyUri != null;

        public HttpHeaders Headers { get; }

        public string HttpMethod { get; set; }

        public string Payload => string.Join("&", PayloadCollection);

        public ICollection<string> PayloadCollection { get; }

        public Uri ProxyUri { get; set; }

        public Uri Url { get; set; }

        public string UserPasswordPair { get; set; }
    }
}
