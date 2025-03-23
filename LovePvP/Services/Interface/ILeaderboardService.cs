using GladiatorHub.Models;
using GladiatorHub.Models.GladiatorHub.Models;
using System.Text.Json;
using static GladiatorHub.Models.BlizzardSettings;

public interface ILeaderboardService
{
    Task<ApiResponseModel<PvpLeaderboardModel>> GetPvpLeaderboardAsync(BlizzardRegion region,int pvpSeasonId, string pvpBracket, int page = 1, int pageSize = 100);
    Task<int> GetCurrentSeasonAsync(BlizzardRegion region);

    Task<ApiResponseModel<List<LeaderboardEntry>>> SoloShuffleAllRatingsAsync(BlizzardRegion region);
}