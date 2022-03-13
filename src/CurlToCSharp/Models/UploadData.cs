namespace CurlToCSharp.Models;

public class UploadData : UploadContent
{
    public UploadData(string content)
        : base(content)
    {
    }

    public UploadData(string content, bool isUrlEncoded)
        : this(null, content, UploadDataType.Inline, isUrlEncoded)
    {
    }

    public UploadData(string content, UploadDataType type)
        : base(content, type)
    {
    }

    public UploadData(string name, string content, UploadDataType type, bool isUrlEncoded)
        : base(name, content, type)
    {
        IsUrlEncoded = isUrlEncoded;
    }

    public bool IsUrlEncoded { get; }

    public string ToQueryStringParameter()
    {
        return string.IsNullOrEmpty(Name) ? Content : $"{Name}={Content}";
    }
}
