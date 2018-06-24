using System.Collections.Generic;

using CurlToCSharp.Models;

using Microsoft.AspNetCore.Mvc;

namespace CurlToCSharp.Controllers
{
    public class HomeController : Controller
    {
        [Route("")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("/error")]
        public IActionResult Error()
        {
            return StatusCode(500, new ConvertResult<string>(new List<string> { "Internal server error, please open an issue" }));
        }
    }
}
