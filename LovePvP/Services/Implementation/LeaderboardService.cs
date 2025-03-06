using GladiatorHub.Models;
using System.Text.Json;

namespace GladiatorHub.Services.Implementation
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly IBlizzardApiService _blizzardApiService;
        private readonly ILogger<LeaderboardService> _logger;
        public LeaderboardService(IBlizzardApiService blizzardApiService, ILogger<LeaderboardService> logger)
        {
            _blizzardApiService = blizzardApiService;
            _logger = logger;
        }



        public async Task<int> GetCurrentSeasonAsync()
        {
            var accessToken = await _blizzardApiService.GetAccessTokenAsync();
            var apiUrl = $"https://us.api.blizzard.com/data/wow/pvp-season/index?namespace=dynamic-us&locale=en_US";

            try
            {
                var jsonDoc = await _blizzardApiService.SafeFetchJsonAsync(apiUrl, accessToken);

                if (jsonDoc.RootElement.TryGetProperty("current_season", out var currentSeason))
                {
                    return GetIntProperty(currentSeason, "id");
                }

                throw new Exception("Current season not found in the response.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching current season.");
                throw new Exception($"Error fetching current season: {ex.Message}", ex);
            }
        }
        public async Task<ApiResponseModel<PvpLeaderboardModel>> GetPvpLeaderboardAsync(int pvpSeasonId, string pvpBracket, int page = 1, int pageSize = 100)
        {
            var accessToken = await _blizzardApiService.GetAccessTokenAsync();
            var apiUrl = $"https://us.api.blizzard.com/data/wow/pvp-season/{pvpSeasonId}/pvp-leaderboard/{pvpBracket}?namespace=dynamic-us&locale=en_US&page={page}&page_size={pageSize}";

            try
            {
                var jsonDoc = await _blizzardApiService.SafeFetchJsonAsync(apiUrl, accessToken);
                var leaderboard = new PvpLeaderboardModel
                {
                    SeasonId = pvpSeasonId,
                    Bracket = pvpBracket
                };

                if (jsonDoc.RootElement.TryGetProperty("entries", out var entriesArray))
                {
                    foreach (var entry in entriesArray.EnumerateArray())
                    {
                        leaderboard.Entries.Add(new LeaderboardEntry
                        {
                            Rank = GetIntProperty(entry, "rank"),
                            Player = new Player
                            {
                                Name = GetStringProperty(entry.GetProperty("character"), "name"),
                                Realm = new Realm
                                {
                                    Slug = GetStringProperty(entry.GetProperty("character").GetProperty("realm"), "slug")
                                }
                            },
                            Faction = new Faction
                            {
                                Type = GetStringProperty(entry.GetProperty("faction"), "type")
                            },
                            Rating = GetIntProperty(entry, "rating"),
                            SeasonMatchStatistics = new SeasonMatchStatistics
                            {
                                Played = GetIntProperty(entry.GetProperty("season_match_statistics"), "played"),
                                Won = GetIntProperty(entry.GetProperty("season_match_statistics"), "won"),
                                Lost = GetIntProperty(entry.GetProperty("season_match_statistics"), "lost")
                            }
                        });
                    }

                    // If the API response provides "next" or similar for pagination, handle it here.
                    if (jsonDoc.RootElement.TryGetProperty("next", out var nextPage))
                    {
                        // Add logic to handle the next page link if provided in the API response.
                    }
                }

                return new ApiResponseModel<PvpLeaderboardModel>
                {
                    Data = leaderboard,
                    Message = "Success"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching leaderboard.");
                return new ApiResponseModel<PvpLeaderboardModel>
                {
                    Data = null,
                    Message = $"Error: {ex.Message}"
                };
            }
        }








        private string GetStringProperty(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : string.Empty;
        }
        private int GetIntProperty(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) ? prop.GetInt32() : 0;
        }
    }
}
