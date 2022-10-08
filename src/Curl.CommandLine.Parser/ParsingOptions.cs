namespace Curl.CommandLine.Parser.Models.Parsing;

public class ParsingOptions
{
    public ParsingOptions()
    {
    }

    public ParsingOptions(int maxUploadFiles)
    {
        MaxUploadFiles = maxUploadFiles;
    }

    public int MaxUploadFiles { get; set; }
}
