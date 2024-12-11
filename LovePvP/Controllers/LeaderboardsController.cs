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

        public async Task<IActionResult> SoloShuffle(string spec)
        {
            try
            {
                
                var currentSeason = await _blizzardApiService.GetCurrentSeasonAsync();
                var leaderboardResponse = await _blizzardApiService.GetPvpLeaderboardAsync(currentSeason, $"shuffle-warrior-fury");

                // Check if leaderboard data or entries are null
                if (leaderboardResponse.Data == null || leaderboardResponse.Data.Entries == null || leaderboardResponse.Data.Entries.Count == 0)
                {
                    return View("Error", new ErrorViewModel { Message = "No leaderboard data available for this spec." });
                }

                return View(leaderboardResponse.Data.Entries); // Pass leaderboard entries to the view
            }
            catch (Exception ex)
            {
                // Log the error for debugging purposes
                Console.WriteLine($"Error in SoloShuffle action: {ex.Message}");

                return View("Error", new ErrorViewModel { Message = $"Error: {ex.Message}" });
            }
        }






        [HttpGet("rated-bgblitz")]
        public IActionResult RatedBgBlitz()
        {
            return View();
        }
    }
}
