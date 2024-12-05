
using Microsoft.AspNetCore.Mvc;

namespace LovePvP.Controllers
{
    public class PlayerController : Controller
    {
        private readonly BlizzardApiService _blizzardApiService;

        public PlayerController(BlizzardApiService blizzardApiService)
        {
            _blizzardApiService = blizzardApiService; 
        }

        public IActionResult Search()
        {
            return View();
        }

        public async Task<IActionResult> PvpSummary(string realmSlug, string characterName)
        {
            var pvpSummary = await _blizzardApiService.GetCharacterPvpSummaryAsync(realmSlug, characterName);

            // Dohvati Solo Shuffle rating i dodaj ga u model
            pvpSummary.SoloShuffleRating = await _blizzardApiService.GetSoloShuffleRatingAsync(realmSlug, characterName);

            return View(pvpSummary);
        }

    }
}

