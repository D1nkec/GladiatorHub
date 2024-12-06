
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
            var apiResponse = await _blizzardApiService.GetPvpLeaderboardAsync(38, "2v2");
            var leaderboard = apiResponse.Data; 

            var entries = leaderboard.Entries;

            return View(entries);
        }


        [HttpGet("3v3")]
        public async Task<IActionResult> ThreeVsThree()
        {
            var apiResponse = await _blizzardApiService.GetPvpLeaderboardAsync(38, "3v3");
            var leaderboard = apiResponse.Data;

            var entries = leaderboard.Entries;

            return View(entries);
        }


        [HttpGet("rated-bg")]
        public async Task<IActionResult> RatedBg()
        {
            var apiResponse = await _blizzardApiService.GetPvpLeaderboardAsync(38, "rbg");
            var leaderboard = apiResponse.Data;

            var entries = leaderboard.Entries;

            return View(entries);
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
