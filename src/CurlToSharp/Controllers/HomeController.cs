using Microsoft.AspNetCore.Mvc;

namespace CurlToSharp.Controllers
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
