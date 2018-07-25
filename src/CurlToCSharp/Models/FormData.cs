namespace CurlToCSharp.Models
{
    public class FormData : UploadContent
    {
        public FormData(string name, string content, UploadDataType type)
            : base(name, content, type)
        {
        }
    }
}
