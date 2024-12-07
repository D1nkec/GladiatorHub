
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

               
                var entries = leaderboard.Entries;
                return View(entries);
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

                var entries = leaderboard.Entries;
                return View(entries);
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

                var entries = leaderboard.Entries;
                return View(entries);
            }
            catch (Exception ex)
            {
                return View("Error", $"An error occurred: {ex.Message}");
            }
        }







        [HttpGet("solo-shuffle")]

        public ActionResult SoloShuffle()
        {

        return View();

        }


        [HttpGet("rated-bgblitz")]
        public ActionResult RatedBgBlitz()
        {

            return View();

        }





    }
}
