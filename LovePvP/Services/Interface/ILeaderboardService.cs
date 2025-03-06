using GladiatorHub.Models;
using GladiatorHub.Models.GladiatorHub.Models;
using System.Text.Json;

public interface ILeaderboardService
{
    Task<ApiResponseModel<PvpLeaderboardModel>> GetPvpLeaderboardAsync(int pvpSeasonId, string pvpBracket, int page = 1, int pageSize = 100);
    Task<int> GetCurrentSeasonAsync();
}