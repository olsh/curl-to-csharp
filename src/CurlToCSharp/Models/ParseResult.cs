using System.Collections.Generic;

namespace CurlToCSharp.Models
{
    public class ConvertResult<T>
        where T : class
    {
        public ConvertResult(T data)
            : this(data, null, null)
        {
        }

        public ConvertResult(ICollection<string> errors)
            : this(null, errors, null)
        {
        }

        public ConvertResult(T data, ICollection<string> errors, ICollection<string> warnings)
        {
            Data = data;
            Errors = errors ?? new HashSet<string>();
            Warnings = warnings ?? new HashSet<string>();
        }

        public T Data { get; }

        public ICollection<string> Errors { get; }

        public bool Success => Errors.Count == 0;

        public ICollection<string> Warnings { get; }

        public void AddWarnings(ICollection<string> warnings)
        {
            AddToCollection(warnings, Warnings);
        }

        private void AddToCollection(ICollection<string> newElements, ICollection<string> collection)
        {
            foreach (var newElement in newElements)
            {
                collection.Add(newElement);
            }
        }
    }
}
