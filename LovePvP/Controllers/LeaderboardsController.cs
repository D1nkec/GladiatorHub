using GladiatorHub.Models;
using GladiatorHub.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GladiatorHub.Controllers
{
    [Route("leaderboards")]
    public class LeaderboardsController : Controller
    {
        private readonly IGameDataService _gameDataService;
        private readonly ILeaderboardService _leaderboardService;
        private readonly ILogger<LeaderboardsController> _logger;
        private const int PageSize = 20; // Number of entries per page

        public LeaderboardsController(IGameDataService gameDataService, ILeaderboardService leaderBoardService, ILogger<LeaderboardsController> logger)
        {
            _gameDataService = gameDataService;
            _leaderboardService = leaderBoardService;
            _logger = logger;
        }

        // Helper method to handle API data fetching with proper error handling
        private async Task<IActionResult> FetchLeaderboardDataAsync(string gameMode, int page)
        {
            try
            {
                var currentSeason = await _leaderboardService.GetCurrentSeasonAsync();
                var apiResponse = await _leaderboardService.GetPvpLeaderboardAsync(currentSeason, gameMode);
                var leaderboard = apiResponse.Data;

                if (leaderboard == null || leaderboard.Entries == null || leaderboard.Entries.Count == 0)
                {
                    _logger.LogWarning($"No leaderboard data found for {gameMode}.");
                    return View("Error", new ErrorViewModel { Message = $"No leaderboard data found for {gameMode}." });
                }

                // Apply pagination
                var paginatedEntries = leaderboard.Entries
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                // Assign faction icons dynamically
                foreach (var entry in paginatedEntries)
                {
                    entry.Faction.IconUrl = _gameDataService.GetFactionIconUrl(entry.Faction.Type);
                }

                // Pagination metadata
                var totalPages = (int)Math.Ceiling((double)leaderboard.Entries.Count / PageSize);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.GameMode = gameMode;

                return View("Leaderboard", paginatedEntries);
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
        public async Task<IActionResult> TwoVsTwo(int page = 1)
        {
            return await FetchLeaderboardDataAsync("2v2", page);
        }

        [HttpGet("3v3")]
        public async Task<IActionResult> ThreeVsThree(int page = 1)
        {
            return await FetchLeaderboardDataAsync("3v3", page);
        }

        [HttpGet("rated-bg")]
        public async Task<IActionResult> RatedBg(int page = 1)
        {
            return await FetchLeaderboardDataAsync("rbg", page);
        }

        [HttpGet("solo-shuffle")]
        public async Task<IActionResult> SoloShuffle(string spec, string klasa, int page = 1)
        {
            try
            {
                if (string.IsNullOrEmpty(spec) || string.IsNullOrEmpty(klasa))
                {
                    _logger.LogWarning("Missing specialization or class in SoloShuffle.");
                    return View("Error", new ErrorViewModel { Message = "Both specialization and class are required." });
                }

                // Format parameters
                var formattedSpec = spec.ToLower().Replace("-", "");
                var formattedKlasa = klasa.ToLower().Replace("-", "");
                var leaderboardKey = $"shuffle-{formattedKlasa}-{formattedSpec}";

                // Fetch leaderboard data
                var currentSeason = await _leaderboardService.GetCurrentSeasonAsync();
                var leaderboardResponse = await _leaderboardService.GetPvpLeaderboardAsync(currentSeason, leaderboardKey);
                var leaderboardEntries = leaderboardResponse?.Data?.Entries;

                if (leaderboardEntries == null || leaderboardEntries.Count == 0)
                {
                    _logger.LogWarning($"No leaderboard data found for specialization {spec} in class {klasa}.");
                    return View("Error", new ErrorViewModel { Message = $"No leaderboard data found for {spec} in {klasa}." });
                }

                // Calculate pagination
                int totalEntries = leaderboardEntries.Count;
                int totalPages = (int)Math.Ceiling((double)totalEntries / PageSize);

                // Ensure page is within bounds
                page = Math.Max(1, Math.Min(page, totalPages)); // Ensures page is at least 1 and at most totalPages

                // Pagination logic
                var paginatedEntries = leaderboardEntries
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                // Ensure faction icons are assigned
                foreach (var entry in paginatedEntries)
                {
                    entry.Faction.IconUrl = _gameDataService.GetFactionIconUrl(entry.Faction.Type);
                }

                // Ensure there is at least 1 page, even if it's empty
                if (totalPages == 0)
                    totalPages = 1;

                // Pass metadata to the view
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.Spec = spec;  // Make sure spec is stored
                ViewBag.Klasa = klasa; // Make sure klasa is stored


                return View("SoloShuffle", paginatedEntries);
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


    }
}
