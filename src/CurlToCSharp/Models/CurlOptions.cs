using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace CurlToCSharp.Models
{
    public class CurlOptions
    {
        public CurlOptions()
        {
            Headers = new HttpRequestHeaders();
            PayloadCollection = new List<string>();
            DataFiles = new List<string>();
            UploadFiles = new List<string>();
        }

        public string CookieValue { get; set; }

        public ICollection<string> DataFiles { get; }

        public ICollection<string> UploadFiles { get; }

        public bool HasCookies => !string.IsNullOrWhiteSpace(CookieValue);

        public bool HasProxy => ProxyUri != null;

        public HttpHeaders Headers { get; }

        public string HttpMethod { get; set; }

        public string Payload => string.Join("&", PayloadCollection);

        public ICollection<string> PayloadCollection { get; }

        public Uri ProxyUri { get; set; }

        public Uri Url { get; set; }

        public string UserPasswordPair { get; set; }

        public Uri GetUrlForFileUpload(string fileName)
        {
            return new Uri(Url, Path.GetFileName(fileName));
        }
    }
}
