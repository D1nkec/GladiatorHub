using GladiatorHub.Models;
using Microsoft.AspNetCore.Mvc;
using GladiatorHub.Models.GladiatorHub.Models;
using Microsoft.Extensions.Caching.Memory;




namespace GladiatorHub.Controllers
{
    public class HomeController : Controller
    {
        private readonly BlizzardApiService _blizzardApiService;
        private readonly ILogger<HomeController> _logger;
        private readonly IMemoryCache _cache;

        public HomeController(BlizzardApiService blizzardApiService, ILogger<HomeController> logger, IMemoryCache cache)
        {
            _logger = logger;
            _blizzardApiService = blizzardApiService;
            _cache = cache;
        }

      
        public async Task<IActionResult> Index()
        {
            try
            {
                // Attempt to get the playable classes from cache first
                List<PlayableClass> playableClasses;
                if (!_cache.TryGetValue("PlayableClasses", out playableClasses))
                {
                    // If not cached, fetch from the API
                    playableClasses = await _blizzardApiService.GetPlayableClassesAsync();

                    // Cache the result for subsequent requests (e.g., cache for 1 hour)
                    _cache.Set("PlayableClasses", playableClasses, TimeSpan.FromHours(1));
                }

                // Fetch specializations for all playable classes in parallel
                var specializationTasks = playableClasses.Select(async playableClass =>
                {
                    // For each class, fetch its specializations and icon URLs in parallel
                    var specializations = await _blizzardApiService.GetSpecializationsForClassAsync(playableClass.Id);

                    // Fetch all specialization icons in parallel
                    var tasks = specializations.Select(async spec =>
                    {
                        var iconUrl = await _blizzardApiService.GetSpecializationIconUrlAsync(spec.Key);
                        return new PlayableSpecialization
                        {
                            Id = spec.Key,
                            Name = spec.Value,
                            IconUrl = iconUrl
                        };
                    });

                    // Return class ID and its list of specializations
                    return new
                    {
                        ClassId = playableClass.Id,
                        Specializations = await Task.WhenAll(tasks)
                    };
                }).ToArray(); // Convert to array to start the tasks in parallel

                // Await all tasks in parallel to get specialization data
                var results = await Task.WhenAll(specializationTasks);

                // Organize the results by class ID
                var classSpecializations = results.ToDictionary(x => x.ClassId, x => x.Specializations.ToList());

                // Pass the class-specialization data to the View
                ViewBag.ClassSpecializations = classSpecializations;

                // Return the playable classes to the View
                return View(playableClasses);
            }
            catch (Exception ex)
            {
                // Log the exception with detailed information
                _logger.LogError(ex, "An error occurred while fetching playable classes and specializations.");

                // Provide a user-friendly error message
                ViewBag.ErrorMessage = "An error occurred while fetching the data. Please try again later.";

                // Return the Index view with an error message
                return View();
            }
        }

        // Action to handle the Solo Shuffle leaderboard redirection
        public IActionResult SoloShuffle(string spec)
        {
            try
            {
                if (string.IsNullOrEmpty(spec))
                {
                    return RedirectToAction("Index");
                }

                // Redirect to the Solo Shuffle leaderboard page on Blizzard website
                return Redirect($"https://worldofwarcraft.com/en-us/pvp/leaderboards/shuffle-{spec}");
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "An error occurred while processing the Solo Shuffle redirect.");

                // Provide a user-friendly error message and redirect to the Index
                ViewBag.ErrorMessage = "An error occurred while processing your request. Please try again later.";
                return RedirectToAction("Index");
            }
        }
    }
}
