using Curl.CommandLine.Parser.Enums;

namespace Curl.CommandLine.Parser.Models
{
    public class FormData : UploadContent
    {
        public FormData(string name, string content, UploadDataType type)
            : base(name, content, type)
        {
        }

        public FormData(string name, string content, UploadDataType type, string contentType, string fileName)
            : base(name, content, type)
        {
            ContentType = contentType;
            FileName = fileName;
        }

        public string ContentType { get; }

        public string FileName { get; set; }
    }
}
