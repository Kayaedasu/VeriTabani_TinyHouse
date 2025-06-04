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

        // Ana sayfa a��ld���nda otomatik Welcome'a y�nlendirir
        public IActionResult Index()
        {
            return RedirectToAction("Welcome");
        }

        // Ho� Geldiniz ekran�
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
