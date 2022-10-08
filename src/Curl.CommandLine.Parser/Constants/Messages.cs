namespace Curl.CommandLine.Parser.Constants;

public class Messages
{
    public const string InvalidCurlCommand = "Invalid curl command";

    public const string UnableParseUrl = "Unable to parse URL";

    public static string GetParameterIsNotSupported(string parameter) => $"Parameter \"{parameter}\" is not supported";
}
