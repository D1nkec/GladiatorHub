using GladiatorHub.Models;
using Microsoft.AspNetCore.Mvc;

namespace GladiatorHub.Controllers
{
    [Route("leaderboards")]
    public class LeaderboardsController : Controller
    {
        private readonly BlizzardApiService _blizzardApiService;

        public LeaderboardsController(BlizzardApiService blizzardApiService)
        {
            _blizzardApiService = blizzardApiService;
        }

        [HttpGet("2v2")]
        public async Task<IActionResult> TwoVsTwo()
        {
            try
            {
                var currentSeason = await _blizzardApiService.GetCurrentSeasonAsync();
                var apiResponse = await _blizzardApiService.GetPvpLeaderboardAsync(currentSeason, "2v2");
                var leaderboard = apiResponse.Data;

                if (leaderboard == null || leaderboard.Entries == null)
                {
                    return View("Error", "Leaderboard data not found.");
                }

                return View(leaderboard.Entries);
            }
            catch (Exception ex)
            {
                return View("Error", $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("3v3")]
        public async Task<IActionResult> ThreeVsThree()
        {
            try
            {
                var currentSeason = await _blizzardApiService.GetCurrentSeasonAsync();
                var apiResponse = await _blizzardApiService.GetPvpLeaderboardAsync(currentSeason, "3v3");
                var leaderboard = apiResponse.Data;

                if (leaderboard == null || leaderboard.Entries == null)
                {
                    return View("Error", "Leaderboard data not found.");
                }

                return View(leaderboard.Entries);
            }
            catch (Exception ex)
            {
                return View("Error", $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("rated-bg")]
        public async Task<IActionResult> RatedBg()
        {
            try
            {
                var currentSeason = await _blizzardApiService.GetCurrentSeasonAsync();
                var apiResponse = await _blizzardApiService.GetPvpLeaderboardAsync(currentSeason, "rbg");
                var leaderboard = apiResponse.Data;

                if (leaderboard == null || leaderboard.Entries == null)
                {
                    return View("Error", "Leaderboard data not found.");
                }

                return View(leaderboard.Entries);
            }
            catch (Exception ex)
            {
                return View("Error", $"An error occurred: {ex.Message}");
            }
        }

        public async Task<IActionResult> SoloShuffle(string spec, string klasa)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(spec))
                {
                    return View("Error", new ErrorViewModel { Message = "Specialization is required." });
                }

                ViewData["Spec"] = spec;

                // Get the current PvP season
                var currentSeason = await _blizzardApiService.GetCurrentSeasonAsync();

                // Format the specialization parameter to match the API endpoint requirements
                var formattedSpec = spec.ToLower().Replace(" ", "-"); // Ensure correct format for API
                var formattedKlasa = klasa.ToLower().Replace(" ", "-");
                // Construct the leaderboard key based on the specialization
                var leaderboardKey = $"shuffle-{klasa}-{formattedSpec}";

                // Fetch leaderboard data from the API
                var leaderboardResponse = await _blizzardApiService.GetPvpLeaderboardAsync(currentSeason, leaderboardKey);

                // Check if leaderboard data or entries are null
                if (leaderboardResponse.Data == null || leaderboardResponse.Data.Entries == null || leaderboardResponse.Data.Entries.Count == 0)
                {
                    return View("Error", new ErrorViewModel { Message = $"No leaderboard data available for specialization: {spec}." });
                }

                // Pass leaderboard entries to the view
                return View(leaderboardResponse.Data.Entries);
            }
            catch (Exception ex)
            {
                // Log the error for debugging purposes
                Console.WriteLine($"Error in SoloShuffle action: {ex.Message}");

                // Return an error view with a friendly message
                return View("Error", new ErrorViewModel { Message = $"An error occurred: {ex.Message}" });
            }
        }






        [HttpGet("rated-bgblitz")]
        public IActionResult RatedBgBlitz()
        {
            return View();
        }
    }
}
