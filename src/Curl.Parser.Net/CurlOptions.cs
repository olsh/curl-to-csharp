using Curl.Parser.Net.Enums;

namespace Curl.Parser.Net.Models;

public class CurlOptions
{
    private readonly IDictionary<string, string> _headers =
        new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

    public CurlOptions()
    {
        UploadData = new List<UploadData>();
        FormData = new List<FormData>();
        UploadFiles = new List<string>();
        CertificateType = CertificateType.Pem;
        HttpVersion = HttpVersion.Default;
    }

    public string CertificateFileName { get; set; }

    public string CertificatePassword { get; set; }

    public CertificateType CertificateType { get; set; }

    public string CookieValue { get; set; }

    public bool ForceGet { get; set; }

    public ICollection<FormData> FormData { get; }

    public bool HasCertificate => !string.IsNullOrEmpty(CertificateFileName);

    public bool HasKey => !string.IsNullOrEmpty(KeyFileName);

    public bool HasCertificatePassword => !string.IsNullOrEmpty(CertificatePassword);

    public bool HasCookies => !string.IsNullOrWhiteSpace(CookieValue);

    public bool HasDataPayload => UploadData.Count > 0;

    public bool HasFilePayload => UploadFiles.Count > 0;

    public bool HttpVersionSpecified => HttpVersion != HttpVersion.Default;

    public bool HasFormPayload => FormData.Count > 0;

    public bool HasHeaders => _headers.Any();

    public bool HasProxy => ProxyUri != null;

    public bool HasProxyUserName => !string.IsNullOrEmpty(ProxyUserName);

    public IReadOnlyDictionary<string, string> Headers => (IReadOnlyDictionary<string, string>)_headers;

    public string HttpMethod { get; set; }

    public HttpVersion HttpVersion { get; set; }

    public bool Insecure { get; set; }

    public bool IsCompressed { get; set; }

    public string KeyFileName { get; set; }

    public KeyType KeyType { get; set; }

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

        if (builder.Uri.IsDefaultPort)
        {
            builder.Port = -1;
        }

        return builder.Uri.AbsoluteUri;
    }

    public string GetHeader(string name)
    {
        if (_headers.TryGetValue(name, out var value))
        {
            return value;
        }

        return null;
    }

    public Uri GetUrlForFileUpload(string fileName)
    {
        return new Uri(Url, Path.GetFileName(fileName));
    }

    public bool HasHeader(string name)
    {
        return _headers.ContainsKey(name);
    }

    public void SetHeader(string name, string value)
    {
        if (!_headers.TryAdd(name, value))
        {
            _headers[name] = value;
        }
    }
}
