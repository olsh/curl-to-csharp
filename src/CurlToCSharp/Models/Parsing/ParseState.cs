namespace CurlToCSharp.Models.Parsing
{
    public class ParseState
    {
        public bool IsCurlCommand { get; set; }

        public string LastUnknownValue { get; set; }
    }
}
