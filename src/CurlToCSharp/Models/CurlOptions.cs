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
            UploadData = new List<UploadData>();
            FormData = new List<FormData>();
            UploadFiles = new List<string>();
        }

        public string CookieValue { get; set; }

        public ICollection<FormData> FormData { get; }

        public bool HasCookies => !string.IsNullOrWhiteSpace(CookieValue);

        public bool HasDataPayload => UploadData.Count > 0;

        public bool HasFilePayload => UploadFiles.Count > 0;

        public bool HasFormPayload => FormData.Count > 0;

        public bool HasProxy => ProxyUri != null;

        public HttpHeaders Headers { get; }

        public string HttpMethod { get; set; }

        public Uri ProxyUri { get; set; }

        public ICollection<UploadData> UploadData { get; }

        public ICollection<string> UploadFiles { get; }

        public Uri Url { get; set; }

        public string UserPasswordPair { get; set; }

        public Uri GetUrlForFileUpload(string fileName)
        {
            return new Uri(Url, Path.GetFileName(fileName));
        }
    }
}
