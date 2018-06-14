using System.Collections.Generic;

namespace CurlToSharp.Models
{
    public class ParseResult<T>
    {
        public ParseResult(T data)
        {
            Data = data;
            Errors = new HashSet<string>();
            Warnings = new HashSet<string>();
        }

        public T Data { get; }

        public ICollection<string> Errors { get; }

        public bool Success => Errors.Count == 0;

        public ICollection<string> Warnings { get; }
    }
}
