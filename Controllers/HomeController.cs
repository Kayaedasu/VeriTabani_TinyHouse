using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TinyHouse.Models;

namespace TinyHouse.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Ana sayfa açýldýðýnda otomatik Welcome'a yönlendirir
        public IActionResult Index()
        {
            return RedirectToAction("Welcome");
        }

        // Hoþ Geldiniz ekraný
        public IActionResult Welcome()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
