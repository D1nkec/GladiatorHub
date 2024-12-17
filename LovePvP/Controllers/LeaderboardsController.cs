using GladiatorHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace GladiatorHub.Controllers
{
    [Route("leaderboards")]
    public class LeaderboardsController : Controller
    {
        private readonly BlizzardApiService _blizzardApiService;
        private readonly ILogger<LeaderboardsController> _logger;

        public LeaderboardsController(BlizzardApiService blizzardApiService, ILogger<LeaderboardsController> logger)
        {
            _blizzardApiService = blizzardApiService;
            _logger = logger;
        }

        // Helper method to handle API data fetching with proper error handling
        private async Task<IActionResult> FetchLeaderboardDataAsync(string gameMode)
        {
            try
            {
                var currentSeason = await _blizzardApiService.GetCurrentSeasonAsync();
                var apiResponse = await _blizzardApiService.GetPvpLeaderboardAsync(currentSeason, gameMode);
                var leaderboard = apiResponse.Data;

                if (leaderboard == null || leaderboard.Entries == null || leaderboard.Entries.Count == 0)
                {
                    _logger.LogWarning($"No leaderboard data found for {gameMode}.");
                    return View("Error", new ErrorViewModel { Message = $"No leaderboard data found for {gameMode}." });
                }

                return View(leaderboard.Entries);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Error fetching leaderboard data from Blizzard API.");
                return View("Error", new ErrorViewModel { Message = "Error connecting to Blizzard API. Please try again later." });
            }
            catch (TimeoutException timeoutEx)
            {
                _logger.LogError(timeoutEx, "Request timed out while fetching leaderboard data.");
                return View("Error", new ErrorViewModel { Message = "The request timed out. Please try again later." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");
                return View("Error", new ErrorViewModel { Message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpGet("2v2")]
        public async Task<IActionResult> TwoVsTwo()
        {
            return await FetchLeaderboardDataAsync("2v2");
        }

        [HttpGet("3v3")]
        public async Task<IActionResult> ThreeVsThree()
        {
            return await FetchLeaderboardDataAsync("3v3");
        }

        [HttpGet("rated-bg")]
        public async Task<IActionResult> RatedBg()
        {
            return await FetchLeaderboardDataAsync("rbg");
        }

        [HttpGet("solo-shuffle")]
        public async Task<IActionResult> SoloShuffle(string spec, string klasa)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(spec) || string.IsNullOrEmpty(klasa))
                {
                    _logger.LogWarning("Missing specialization or class in SoloShuffle.");
                    return View("Error", new ErrorViewModel { Message = "Both specialization and class are required." });
                }

                // Format the specialization and class for the API
                var formattedSpec = spec.ToLower().Replace(" ", "-");
                var formattedKlasa = klasa.ToLower().Replace(" ", "-");

                // Construct the leaderboard key
                var leaderboardKey = $"shuffle-{formattedKlasa}-{formattedSpec}";

                // Get the current PvP season
                var currentSeason = await _blizzardApiService.GetCurrentSeasonAsync();
                var leaderboardResponse = await _blizzardApiService.GetPvpLeaderboardAsync(currentSeason, leaderboardKey);

                // Check for missing or empty leaderboard data
                if (leaderboardResponse.Data == null || leaderboardResponse.Data.Entries == null || leaderboardResponse.Data.Entries.Count == 0)
                {
                    _logger.LogWarning($"No leaderboard data found for specialization {spec} in class {klasa}.");
                    return View("Error", new ErrorViewModel { Message = $"No leaderboard data found for {spec} in {klasa}." });
                }

                return View(leaderboardResponse.Data.Entries);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Error fetching Solo Shuffle leaderboard data from Blizzard API.");
                return View("Error", new ErrorViewModel { Message = "Error connecting to Blizzard API. Please try again later." });
            }
            catch (TimeoutException timeoutEx)
            {
                _logger.LogError(timeoutEx, "Request timed out while fetching Solo Shuffle leaderboard data.");
                return View("Error", new ErrorViewModel { Message = "The request timed out. Please try again later." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching Solo Shuffle leaderboard.");
                return View("Error", new ErrorViewModel { Message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpGet("rated-bgblitz")]
        public IActionResult RatedBgBlitz()
        {
            return View();
        }
    }
}
