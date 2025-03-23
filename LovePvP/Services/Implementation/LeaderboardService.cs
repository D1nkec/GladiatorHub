using GladiatorHub.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using static GladiatorHub.Models.BlizzardSettings;

namespace GladiatorHub.Services.Implementation
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly BlizzardSettings _settings;
        private readonly IBlizzardApiService _blizzardApiService;
        private readonly ILogger<LeaderboardService> _logger;
        public LeaderboardService(IOptions<BlizzardSettings> settings, IBlizzardApiService blizzardApiService, ILogger<LeaderboardService> logger)
        {
            _settings = settings.Value;
            _blizzardApiService = blizzardApiService;
            _logger = logger;
        }



        public async Task<int> GetCurrentSeasonAsync(BlizzardRegion region = BlizzardRegion.US)
        {
            if (!_settings.ApiBaseUrls.TryGetValue(region, out var apiBaseUrl))
            {
                throw new Exception($"Unsupported region: {region}");
            }

            var accessToken = await _blizzardApiService.GetAccessTokenAsync();
            var apiUrl = $"{apiBaseUrl}/data/wow/pvp-season/index?namespace=dynamic-{region.ToString().ToLower()}&locale=en_US";


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
        public async Task<ApiResponseModel<PvpLeaderboardModel>> GetPvpLeaderboardAsync(BlizzardRegion region, int pvpSeasonId, string pvpBracket, int page = 1, int pageSize = 100)
        {
            if (!_settings.ApiBaseUrls.TryGetValue(region, out var apiBaseUrl))
            {
                throw new Exception($"Unsupported region: {region}");
            }

            var accessToken = await _blizzardApiService.GetAccessTokenAsync();
            var apiUrl = $"{apiBaseUrl}/data/wow/pvp-season/{pvpSeasonId}/pvp-leaderboard/{pvpBracket}?namespace=dynamic-{region.ToString().ToLower()}&locale=en_US&page={page}&page_size={pageSize}";

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


        public async Task<ApiResponseModel<List<LeaderboardEntry>>> SoloShuffleAllRatingsAsync(BlizzardRegion region)
        {
            if (!_settings.ApiBaseUrls.TryGetValue(region, out var apiBaseUrl))
            {
                throw new Exception($"Unsupported region: {region}");
            }

            var accessToken = await _blizzardApiService.GetAccessTokenAsync();
            var seasonId = await GetCurrentSeasonAsync(region);
            var apiUrl = $"{apiBaseUrl}/data/wow/pvp-season/{seasonId}/pvp-leaderboard/?namespace=dynamic-{region.ToString().ToLower()}&locale=en_US";

            try
            {
                var jsonDoc = await _blizzardApiService.SafeFetchJsonAsync(apiUrl, accessToken);
                var leaderboards = jsonDoc.RootElement.GetProperty("leaderboards");

                List<LeaderboardEntry> allRatings = new List<LeaderboardEntry>();

                foreach (var leaderboard in leaderboards.EnumerateArray())
                {
                    string leaderboardUrl = leaderboard.GetProperty("key").GetProperty("href").GetString();
                    string bracket = leaderboard.GetProperty("name").GetString();

                    // Fetch ratings from each leaderboard
                    var leaderboardEntries = await GetPvpLeaderboardAsync(region, seasonId, bracket);
                    if (leaderboardEntries.Data != null)
                    {
                        allRatings.AddRange(leaderboardEntries.Data.Entries);
                    }
                }

                return new ApiResponseModel<List<LeaderboardEntry>>
                {
                    Data = allRatings,
                    Message = "Success"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all PvP leaderboards.");
                return new ApiResponseModel<List<LeaderboardEntry>>
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
