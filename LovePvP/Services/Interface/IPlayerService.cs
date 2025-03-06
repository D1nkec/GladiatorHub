
using GladiatorHub.Models;

public interface IPlayerService
{
    Task<PvpSummaryModel> GetCharacterPvpSummaryAsync(string realmSlug, string characterName);
    Task<Dictionary<string, int>> GetSoloShuffleRatingAsync(string realmSlug, string characterName);
    Task<Dictionary<string, int>> GetArenaRatingAsync(string realmSlug, string characterName);

    Task<Dictionary<string, int>> GetBgBlitzRatingAsync(string realmSlug, string characterName);
}