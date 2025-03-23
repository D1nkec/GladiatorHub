using GladiatorHub.Models;
using GladiatorHub.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using static GladiatorHub.Models.BlizzardSettings;

namespace GladiatorHub.Controllers
{
    [Route("leaderboards")]
    public class LeaderboardsController : Controller
    {
        private readonly IGameDataService _gameDataService;
        private readonly ILeaderboardService _leaderboardService;
        private readonly ILogger<LeaderboardsController> _logger;
        private const int PageSize = 20;
        public LeaderboardsController(IGameDataService gameDataService, ILeaderboardService leaderboardService, ILogger<LeaderboardsController> logger)
        {
            _gameDataService = gameDataService;
            _leaderboardService = leaderboardService;
            _logger = logger;
        }

        // Generic method for fetching leaderboard data with proper error handling
        private async Task<IActionResult> FetchLeaderboardDataAsync(string gameMode, BlizzardRegion region, int page)
        {
            try
            {
                // Fetch current season using the region.
                var currentSeason = await _leaderboardService.GetCurrentSeasonAsync(region);

                // Use the region to call the API.
                var apiResponse = await _leaderboardService.GetPvpLeaderboardAsync(region, currentSeason, gameMode);
                var leaderboard = apiResponse?.Data;

                if (leaderboard?.Entries == null || leaderboard.Entries.Count == 0)
                {
                    _logger.LogWarning($"No leaderboard data found for {gameMode} in {region}.");
                    return View("Error", new ErrorViewModel { Message = $"No leaderboard data found for {gameMode} in {region}." });
                }

                // Apply pagination
                int totalPages = (int)Math.Ceiling((double)leaderboard.Entries.Count / PageSize);
                page = Math.Max(1, Math.Min(page, totalPages));

                var paginatedEntries = leaderboard.Entries
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                // Assign faction icons dynamically
                foreach (var entry in paginatedEntries)
                {
                    entry.Faction.IconUrl = _gameDataService.GetFactionIconUrl(entry.Faction.Type);
                }

                // Ensure at least 1 page exists
                if (totalPages == 0) totalPages = 1;

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.GameMode = gameMode;
                ViewBag.Region = region;

                return View("Leaderboard", paginatedEntries);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, $"Error fetching {gameMode} leaderboard data from Blizzard API for {region}.");
                return View("Error", new ErrorViewModel { Message = "Error connecting to Blizzard API. Please try again later." });
            }
            catch (TimeoutException timeoutEx)
            {
                _logger.LogError(timeoutEx, $"Request timed out while fetching {gameMode} leaderboard data for {region}.");
                return View("Error", new ErrorViewModel { Message = "The request timed out. Please try again later." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred while fetching {gameMode} leaderboard for {region}.");
                return View("Error", new ErrorViewModel { Message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpGet("2v2")]
        public async Task<IActionResult> TwoVsTwo(BlizzardRegion region = BlizzardRegion.US, int page = 1)
        {
            ViewBag.Region = region.ToString();
            return await FetchLeaderboardDataAsync("2v2", region, page);
        }


        [HttpGet("3v3")]
        public async Task<IActionResult> ThreeVsThree(BlizzardRegion region = BlizzardRegion.US, int page = 1)
        {
            ViewBag.Region = region.ToString();
            return await FetchLeaderboardDataAsync("3v3", region, page);
        }

        [HttpGet("rated-bg")]
        public async Task<IActionResult> RatedBg(BlizzardRegion region = BlizzardRegion.US, int page = 1)
        {
            ViewBag.Region = region.ToString();
            return await FetchLeaderboardDataAsync("rbg", region, page);
        }


        [HttpGet("solo-shuffle")]
        public async Task<IActionResult> SoloShuffle(string spec, string klasa, BlizzardRegion region = BlizzardRegion.US, int page = 1)
        {
            try
            {
                if (string.IsNullOrEmpty(spec) || string.IsNullOrEmpty(klasa))
                {
                    _logger.LogWarning("Missing specialization or class in Solo Shuffle.");
                    return View("Error", new ErrorViewModel { Message = "Both specialization and class are required." });
                }

                // Format parameters
                var formattedSpec = spec.ToLower().Replace("-", "");
                var formattedKlasa = klasa.ToLower().Replace("-", "");
                var leaderboardKey = $"shuffle-{formattedKlasa}-{formattedSpec}";

                // Fetch leaderboard data
                var currentSeason = await _leaderboardService.GetCurrentSeasonAsync(region);
                var leaderboardResponse = await _leaderboardService.GetPvpLeaderboardAsync(region,currentSeason, leaderboardKey);
                var leaderboardEntries = leaderboardResponse?.Data?.Entries;

                if (leaderboardEntries == null || leaderboardEntries.Count == 0)
                {
                    _logger.LogWarning($"No Solo Shuffle data found for {spec} ({klasa}) in {region}.");
                    return View("Error", new ErrorViewModel { Message = $"No leaderboard data found for {spec} in {klasa} ({region})." });
                }

                // Pagination logic
                int totalEntries = leaderboardEntries.Count;
                int totalPages = (int)Math.Ceiling((double)totalEntries / PageSize);
                page = Math.Max(1, Math.Min(page, totalPages));

                var paginatedEntries = leaderboardEntries
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                // Assign faction icons dynamically
                foreach (var entry in paginatedEntries)
                {
                    entry.Faction.IconUrl = _gameDataService.GetFactionIconUrl(entry.Faction.Type);
                }

                // Ensure at least 1 page exists
                if (totalPages == 0) totalPages = 1;

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.Spec = spec;
                ViewBag.Klasa = klasa;
                ViewBag.Region = region;

                return View("SoloShuffle", paginatedEntries);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, $"Error fetching Solo Shuffle leaderboard data from Blizzard API for {region}.");
                return View("Error", new ErrorViewModel { Message = "Error connecting to Blizzard API. Please try again later." });
            }
            catch (TimeoutException timeoutEx)
            {
                _logger.LogError(timeoutEx, $"Request timed out while fetching Solo Shuffle leaderboard data for {region}.");
                return View("Error", new ErrorViewModel { Message = "The request timed out. Please try again later." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred while fetching Solo Shuffle leaderboard for {region}.");
                return View("Error", new ErrorViewModel { Message = "An unexpected error occurred. Please try again later." });
            }
        }


        [HttpGet("solo-shuffle-all-ratings")]
        public async Task<IActionResult> SoloShuffleAll()
        {
            var leaderboard = await _leaderboardService.SoloShuffleAllRatingsAsync(BlizzardRegion.US);

            if (leaderboard.Data == null || !leaderboard.Data.Any()) // ✅ No need for .Entries
                return BadRequest(leaderboard.Message);
            
            var leaderboardEntries = leaderboard.Data
                                                .GroupBy(entry => entry.Player.Name)   
                                                .Select(group => group.First())         
                                                .OrderBy(entry => entry.Rank)           
                                                .ToList();

            return View(leaderboard.Data); 
        }









    }
}
