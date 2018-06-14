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
    }
}
