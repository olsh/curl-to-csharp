namespace CurlToCSharp.Models
{
    public abstract class UploadContent
    {
        protected UploadContent(string content)
            : this(null, content, UploadDataType.Inline)
        {
        }

        protected UploadContent(string content, UploadDataType type)
            : this(null, content, type)
        {
        }

        protected UploadContent(string name, string content, UploadDataType type)
        {
            Content = content;
            Type = type;
            Name = name;
        }

        public string Content { get; }

        public bool HasName => !string.IsNullOrWhiteSpace(Name);

        public bool IsFile => Type == UploadDataType.BinaryFile || Type == UploadDataType.InlineFile;

        public string Name { get; }

        public UploadDataType Type { get; }
    }
}
