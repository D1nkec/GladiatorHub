using LovePvP.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LovePvP.Controllers
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

            // Retrieve Solo Shuffle rating and add it to the model
            pvpSummary.SoloShuffleRating = await _blizzardApiService.GetSoloShuffleRatingAsync(realmSlug, characterName);

            return View(pvpSummary);
            }





        }
    }

