using Curl.Converter.Net;
using Curl.Parser.Net;
using Curl.Parser.Net.Models;

using CurlToCSharp.Models;

using Microsoft.AspNetCore.Mvc;

namespace CurlToCSharp.Controllers;

[Route("[controller]")]
public class ConvertController : Controller
{
    private readonly ICurlConverter _curlConverterService;

    private readonly ICurlParser _commandLineCurlParser;

    public ConvertController(ICurlConverter curlConverterService, ICurlParser commandLineCurlParser)
    {
        _curlConverterService = curlConverterService;
        _commandLineCurlParser = commandLineCurlParser;
    }

    [HttpPost]
    public IActionResult Post([FromBody] ConvertModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(
                new ConvertResult<ConvertModel>(
                    ModelState.SelectMany(r => r.Value.Errors.Select(e => e.ErrorMessage))
                        .ToArray()));
        }

        var parseResult = _commandLineCurlParser.Parse(new Span<char>(model.Curl.ToCharArray()));
        if (!parseResult.Success)
        {
            return BadRequest(parseResult);
        }

        var csharp = _curlConverterService.ToCsharp(parseResult.Data);
        csharp.AddWarnings(parseResult.Warnings);

        return Ok(csharp);
    }
}
