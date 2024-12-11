using GladiatorHub.Models;
using GladiatorHub.Mappings;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using GladiatorHub.Models.GladiatorHub.Models;

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
            var classSpecializations = new Dictionary<int, List<PlayableSpecialization>>();

            // Map classes to their specializations
            foreach (var classEntry in ClassSpecializationMappings.ClassSpecializations)
            {
                var specializations = new List<PlayableSpecialization>();

                foreach (var specialization in classEntry.Value)
                {
                    var iconUrl = await _blizzardApiService.GetSpecializationIconUrlAsync(specialization.Key);
                    specializations.Add(new PlayableSpecialization
                    {
                        Id = specialization.Key,
                        Name = specialization.Value,
                        IconUrl = iconUrl
                    });
                }

                classSpecializations[classEntry.Key] = specializations;
            }

            ViewBag.ClassSpecializations = classSpecializations;
            return View(classIcons);
        }
    }
}
