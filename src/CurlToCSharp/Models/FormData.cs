namespace CurlToCSharp.Models
{
    public class UploadData
    {
        public UploadData(string content)
            : this(content, DataContentType.Inline)
        {
        }

        public UploadData(string content, bool isUrlEncoded)
            : this(null, content, DataContentType.Inline, isUrlEncoded)
        {
        }

        public UploadData(string content, DataContentType contentType)
            : this(null, content, contentType, false)
        {
        }

        public UploadData(string name, string content, DataContentType contentType, bool isUrlEncoded)
        {
            Content = content;
            ContentType = contentType;
            Name = name;
            IsUrlEncoded = isUrlEncoded;
        }

        public string Content { get; }

        public DataContentType ContentType { get; }

        public bool IsFile => ContentType == DataContentType.BinaryFile || ContentType == DataContentType.EscapedFile;

        public bool HasName => !string.IsNullOrWhiteSpace(Name);

        public bool IsUrlEncoded { get; }

        public string Name { get; }
    }
}
