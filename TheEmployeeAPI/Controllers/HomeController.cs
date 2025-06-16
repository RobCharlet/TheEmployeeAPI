using Microsoft.AspNetCore.Mvc;

namespace TheEmployeeAPI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
} 