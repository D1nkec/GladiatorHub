using GladiatorHub.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GladiatorHub.Controllers
{
    public class HomeController : Controller
    {
        private readonly BlizzardApiService _blizzardApiService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(BlizzardApiService blizzardApiService, ILogger<HomeController> logger)
        {
            _logger = logger;
            _blizzardApiService = blizzardApiService;
        }






        public async Task<IActionResult> Index()
        {
            var classIcons = await _blizzardApiService.GetAllClassIconsAsync();
            return View(classIcons);
        }

        public async Task<IActionResult> Specializations()
        {
            var specializationIcons = await _blizzardApiService.GetAllSpecializationIconsAsync();
            return View(specializationIcons);
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