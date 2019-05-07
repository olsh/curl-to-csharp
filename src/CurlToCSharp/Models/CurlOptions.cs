using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.WebUtilities;

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
            CertificateType = CertificateType.Pem;
        }

        public string CertificateFileName { get; set; }

        public string CertificatePassword { get; set; }

        public CertificateType CertificateType { get; set; }

        public string CookieValue { get; set; }

        public bool ForceGet { get; set; }

        public ICollection<FormData> FormData { get; }

        public bool HasCertificate => !string.IsNullOrEmpty(CertificateFileName);

        public bool HasCertificatePassword => !string.IsNullOrEmpty(CertificatePassword);

        public bool HasCookies => !string.IsNullOrWhiteSpace(CookieValue);

        public bool HasDataPayload => UploadData.Count > 0;

        public bool HasFilePayload => UploadFiles.Count > 0;

        public bool HasFormPayload => FormData.Count > 0;

        public bool HasProxy => ProxyUri != null;

        public bool HasProxyUserName => !string.IsNullOrEmpty(ProxyUserName);

        public HttpHeaders Headers { get; }

        public string HttpMethod { get; set; }

        public bool Insecure { get; set; }

        public string ProxyPassword { get; set; }

        public Uri ProxyUri { get; set; }

        public string ProxyUserName { get; set; }

        public ICollection<UploadData> UploadData { get; }

        public ICollection<string> UploadFiles { get; }

        public Uri Url { get; set; }

        public bool UseDefaultProxyCredentials { get; set; }

        public string UserPasswordPair { get; set; }

        public string GetFullUrl()
        {
            if (!ForceGet)
            {
                return Url.ToString();
            }

            var builder = new UriBuilder(Url)
                              {
                                  Query = string.Join("&", UploadData.Select(d => d.ToQueryStringParameter()))
                              };

            return builder.ToString();
        }

        public Uri GetUrlForFileUpload(string fileName)
        {
            return new Uri(Url, Path.GetFileName(fileName));
        }
    }
}
