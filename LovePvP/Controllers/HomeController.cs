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
            // Fetch all playable classes
            var playableClasses = await _blizzardApiService.GetPlayableClassesAsync();

            // Fetch specializations for each class asynchronously
            var classSpecializations = new Dictionary<int, List<PlayableSpecialization>>();

            foreach (var playableClass in playableClasses)
            {
                // Await asynchronous operation for specializations
                var specializations = await _blizzardApiService.GetSpecializationsForClassAsync(playableClass.Id);

                if (specializations != null)
                {
                    // Map specializations asynchronously
                    var specializationList = new List<PlayableSpecialization>();
                    foreach (var spec in specializations)
                    {
                        var specializationIconUrl = await _blizzardApiService.GetSpecializationIconUrlAsync(spec.Key);
                        specializationList.Add(new PlayableSpecialization
                        {
                            Id = spec.Key,
                            Name = spec.Value,
                            IconUrl = specializationIconUrl
                        });
                    }

                    // Add to the dictionary
                    classSpecializations[playableClass.Id] = specializationList;
                }
            }

            // Pass the specializations data to the view
            ViewBag.ClassSpecializations = classSpecializations;
            return View(playableClasses);
        }

        public IActionResult SoloShuffle(string spec)
        {
            // Redirect to the Solo Shuffle leaderboard page
            return Redirect($"https://worldofwarcraft.com/en-us/pvp/leaderboards/shuffle-{spec}");
        }
    }
}