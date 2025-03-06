using GladiatorHub.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GladiatorHub.Controllers
{
    public class PlayerController : Controller
    {
        private readonly IPlayerService _playerService;

        public PlayerController(IPlayerService playerService)
        {
            _playerService = playerService;
        }


        [HttpGet]
        public async Task<IActionResult> PvpSummary(string realmSlug, string characterName)
        {
            var pvpSummary = await _playerService.GetCharacterPvpSummaryAsync(realmSlug, characterName);
            pvpSummary.SoloShuffleRating = await _playerService.GetSoloShuffleRatingAsync(realmSlug, characterName);
            pvpSummary.ArenaRating = await _playerService.GetArenaRatingAsync(realmSlug, characterName);
            pvpSummary.BgBlitzRating = await _playerService.GetBgBlitzRatingAsync(realmSlug, characterName);
            return View(pvpSummary);
        }

    }
}

