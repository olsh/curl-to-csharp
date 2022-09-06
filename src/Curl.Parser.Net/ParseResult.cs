namespace Curl.Parser.Net.Models;

public class ConvertResult<T>
    where T : class
{
    public ConvertResult() : this(default(T))
    {
    }

    public ConvertResult(T data)
        : this(data, new HashSet<string>(), new HashSet<string>())
    {
    }

    public ConvertResult(ICollection<string> errors)
        : this(default(T), errors, new HashSet<string>())
    {
    }

    public ConvertResult(T data, ICollection<string> errors, ICollection<string> warnings)
    {
        Data = data;
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
    }

    public T Data { get; set; }

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
