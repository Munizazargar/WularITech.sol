using Microsoft.AspNetCore.Mvc;

namespace WularItech_solutions.Controllers
{
    public class ServicesController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
