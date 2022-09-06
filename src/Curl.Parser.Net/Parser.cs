using Curl.Parser.Net.Constants;
using Curl.Parser.Net.Extensions;
using Curl.Parser.Net.Models;
using Curl.Parser.Net.Models.Parsing;

namespace Curl.Parser.Net;

public class Parser : IParser
{
    private readonly IEnumerable<ParameterEvaluator> _evaluators;

    public Parser(ParsingOptions parsingOptions)
        : this(EvaluatorProvider.All(parsingOptions))
    {
    }

    private Parser(IEnumerable<ParameterEvaluator> evaluators)
    {
        _evaluators = evaluators;
    }

    public ConvertResult<CurlOptions> Parse(string commandLine)
    {
        return Parse(new Span<char>(commandLine.ToCharArray()));
    }

    public ConvertResult<CurlOptions> Parse(Span<char> commandLine)
    {
        if (commandLine.IsEmpty)
        {
            throw new ArgumentException("The command line is empty.", nameof(commandLine));
        }

        var parseResult = new ConvertResult<CurlOptions>(new CurlOptions());
        var parseState = new ParseState();
        while (!commandLine.IsEmpty)
        {
            commandLine = commandLine.TrimCommandLine();
            if (commandLine.IsEmpty)
            {
                break;
            }

            if (commandLine.IsParameter())
            {
                var parameter = commandLine.ReadParameter();
                EvaluateParameter(parameter, ref commandLine, parseResult);
            }
            else
            {
                var value = commandLine.ReadValue();
                EvaluateValue(parseResult, parseState, value);
            }
        }

        PostParsing(parseResult, parseState);

        return parseResult;
    }

    private static void EvaluateValue(ConvertResult<CurlOptions> convertResult, ParseState parseState, Span<char> value)
    {
        var valueString = value.ToString();
        if (string.Equals(valueString, "curl", StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        if (convertResult.Data.Url == null && Uri.TryCreate(valueString, UriKind.Absolute, out var url)
                                           && !string.IsNullOrEmpty(url.Host))
        {
            convertResult.Data.Url = url;
        }
        else
        {
            parseState.LastUnknownValue = valueString;
        }
    }

    private void EvaluateParameter(Span<char> parameter, ref Span<char> commandLine, ConvertResult<CurlOptions> convertResult)
    {
        var par = parameter.ToString();

        foreach (var evaluator in _evaluators)
        {
            if (evaluator.CanEvaluate(par))
            {
                evaluator.Evaluate(ref commandLine, convertResult);

                return;
            }
        }

        convertResult.Warnings.Add(Messages.GetParameterIsNotSupported(par));
    }

    private void PostParsing(ConvertResult<CurlOptions> result, ParseState state)
    {
        if (result.Data.Url == null
            && !string.IsNullOrWhiteSpace(state.LastUnknownValue)
            && Uri.TryCreate($"http://{state.LastUnknownValue}", UriKind.Absolute, out Uri url))
        {
            result.Data.Url = url;
        }

        // This option overrides -F, --form and -I, --head and -T, --upload-file.
        if (result.Data.HasDataPayload)
        {
            result.Data.UploadFiles.Clear();
            result.Data.FormData.Clear();
        }

        if (result.Data.HasFormPayload)
        {
            result.Data.UploadFiles.Clear();
        }

        // If used in combination with -I, --head, the POST data will instead be appended to the URL with a HEAD request.
        if (result.Data.ForceGet && result.Data.HttpMethod != HttpMethod.Head.ToString().ToUpper())
        {
            result.Data.HttpMethod = HttpMethod.Get.ToString()
                .ToUpper();
        }

        if (result.Data.HttpMethod == null)
        {
            if (result.Data.HasDataPayload)
            {
                result.Data.HttpMethod = HttpMethod.Post.ToString()
                    .ToUpper();
            }
            else if (result.Data.HasFormPayload)
            {
                result.Data.HttpMethod = HttpMethod.Post.ToString()
                    .ToUpper();
            }
            else if (result.Data.HasFilePayload)
            {
                result.Data.HttpMethod = HttpMethod.Put.ToString()
                    .ToUpper();
            }
            else
            {
                result.Data.HttpMethod = HttpMethod.Get.ToString()
                    .ToUpper();
            }
        }

        if (!result.Data.HasHeader(HeaderNames.ContentType) && result.Data.HasDataPayload)
        {
            result.Data.SetHeader(HeaderNames.ContentType, HeaderValues.ContentTypeWwwForm);
        }

        if (result.Data.Url == null)
        {
            result.Errors.Add(Messages.UnableParseUrl);
        }
    }
}
