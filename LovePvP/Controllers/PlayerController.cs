using GladiatorHub.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GladiatorHub.Controllers
{
    public class PlayerController : Controller
    {
        private readonly BlizzardApiService _blizzardApiService;

        public PlayerController(BlizzardApiService blizzardApiService)
        {
            _blizzardApiService = blizzardApiService;
        }


        [HttpGet]
        public async Task<IActionResult> PvpSummary(string realmSlug, string characterName)
        {
            var pvpSummary = await _blizzardApiService.GetCharacterPvpSummaryAsync(realmSlug, characterName);

            pvpSummary.SoloShuffleRating = await _blizzardApiService.GetSoloShuffleRatingAsync(realmSlug, characterName);
            return View(pvpSummary);
        }

    }
}

