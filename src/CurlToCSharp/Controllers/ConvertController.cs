using System;
using System.Threading.Tasks;

using CurlToCSharp.Models;
using CurlToCSharp.Services;

using Microsoft.AspNetCore.Mvc;

namespace CurlToCSharp.Controllers
{
    [Route("[controller]")]
    public class ConvertController : Controller
    {
        private readonly IConverterService _converterService;

        private readonly ICommandLineParser _commandLineParser;

        public ConvertController(IConverterService converterService, ICommandLineParser commandLineParser)
        {
            _converterService = converterService;
            _commandLineParser = commandLineParser;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ConvertModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(model);
            }

            var parseResult = _commandLineParser.Parse(new Span<char>(model.Curl.ToCharArray()));
            if (!parseResult.Success)
            {
                return BadRequest(parseResult);
            }

            var csharp = _converterService.ToCsharp(parseResult.Data);

            return Ok(csharp);
        }
    }
}
