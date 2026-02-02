using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SystemPurchaseAccGame.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        public async Task<IActionResult> HomePage()
        {
            await Task.CompletedTask;

            return View();
        }
       
    }
}
