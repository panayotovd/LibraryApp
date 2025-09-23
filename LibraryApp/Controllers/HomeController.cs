using Microsoft.AspNetCore.Mvc;

namespace LibraryApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
        public IActionResult About() => View();
        public IActionResult Error() => View();
        public new IActionResult StatusCode(int code)
        {
            ViewBag.Code = code;
            return View("StatusCode");
        }
    }
}
