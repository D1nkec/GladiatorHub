
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
            var leaderboard = await _blizzardApiService.GetPvpLeaderboardAsync(33, "2v2");
            var entries = leaderboard.Entries; // Izdvojite samo listu unosa

            return View(entries); // Poslati samo listu unosa
        }




        [HttpGet("3v3")]
        public IActionResult ThreeVsThree()
        {
            return View();
        } 

        [HttpGet("solo-shuffle")]
        public IActionResult SoloShuffle()
        {
            return View();
        }
        [HttpGet("rated-bgblitz")]
        public IActionResult RatedBgBlitz()
        {
            return View();
        }
        
    }
}
