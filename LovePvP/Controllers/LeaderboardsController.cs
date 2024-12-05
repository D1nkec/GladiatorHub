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



        //[HttpGet("2v2")]
        //public async Task<IActionResult> TwoVsTwo(int page = 1, int pvpSeasonId = 33, string pvpBracket = "2v2")
        //{
        //    const int pageSize = 20;
        //    var leaderboardData = await _blizzardApiService.GetPvpLeaderboardAsync(pvpSeasonId, pvpBracket, page, pageSize);

        //    var totalPages = (int)Math.Ceiling((double)leaderboardData.TotalCount / pageSize);

        //    var viewModel = new PvpLeaderboardViewModel
        //    {
        //        Leaderboard = leaderboardData.Leaderboard,
        //        CurrentPage = page,
        //        TotalPages = totalPages,
        //        PvpBracket = pvpBracket,
        //        PvpSeasonId = pvpSeasonId
        //    };

        //    return View(viewModel);
        //}


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
