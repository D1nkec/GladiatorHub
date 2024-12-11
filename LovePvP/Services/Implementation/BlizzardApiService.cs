using GladiatorHub.Mappings;
using GladiatorHub.Models;
using GladiatorHub.Models.GladiatorHub.Models;
using System.Net.Http.Headers;
using System.Text.Json;

public class BlizzardApiService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public BlizzardApiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
    }


    // Dohvaćanje pristupnog tokena
    public async Task<string> GetAccessTokenAsync()
    {
        var clientId = _configuration["Blizzard:ClientId"];
        var clientSecret = _configuration["Blizzard:ClientSecret"];
        var tokenUrl = _configuration["Blizzard:TokenUrl"];

        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
        var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        return jsonDocument.RootElement.GetProperty("access_token").GetString();
    }

    // Pomoćna metoda za dohvaćanje JSON odgovora
    private async Task<JsonDocument> FetchJsonAsync(string url, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(jsonString);
    }

    // Pomoćne metode za dohvaćanje vrijednosti
    private string GetStringProperty(JsonElement element, string propertyName, string defaultValue = "Unknown")
        => element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : defaultValue;

    private int GetIntProperty(JsonElement element, string propertyName, int defaultValue = 0)
        => element.TryGetProperty(propertyName, out var prop) ? prop.GetInt32() : defaultValue;



    // GET PvP Summary
    public async Task<PvpSummaryModel> GetCharacterPvpSummaryAsync(string realmSlug, string characterName)
    {
        var accessToken = await GetAccessTokenAsync();
        var apiUrl = $"{_configuration["Blizzard:ApiBaseUrl"]}/profile/wow/character/{realmSlug}/{characterName}/pvp-summary?namespace=profile-us&locale=en_US";

        var jsonData = await FetchJsonAsync(apiUrl, accessToken);

        return new PvpSummaryModel
        {
            CharacterName = GetStringProperty(jsonData.RootElement.GetProperty("character"), "name"),
            RealmName = GetStringProperty(jsonData.RootElement.GetProperty("character").GetProperty("realm"), "name"),
            HonorLevel = GetIntProperty(jsonData.RootElement, "honor_level"),
            HonorableKills = GetIntProperty(jsonData.RootElement, "honorable_kills"),
            PvpMapStatistics = jsonData.RootElement.TryGetProperty("pvp_map_statistics", out var mapStatsProp) && mapStatsProp.ValueKind == JsonValueKind.Array
                ? mapStatsProp.EnumerateArray().Select(map => new MapStatistics
                {
                    MapName = GetStringProperty(map.GetProperty("world_map"), "name"),
                    Played = GetIntProperty(map.GetProperty("match_statistics"), "played"),
                    Won = GetIntProperty(map.GetProperty("match_statistics"), "won"),
                    Lost = GetIntProperty(map.GetProperty("match_statistics"), "lost")
                }).ToList()
                : new List<MapStatistics>()
        };
    }


    // GET Solo Shuffle rating
    public async Task<Dictionary<string, int>> GetSoloShuffleRatingAsync(string realmSlug, string characterName)
    {
        var accessToken = await GetAccessTokenAsync();
        var apiUrl = $"{_configuration["Blizzard:ApiBaseUrl"]}/profile/wow/character/{realmSlug}/{characterName}/pvp-summary?namespace=profile-us";

        var jsonDoc = await FetchJsonAsync(apiUrl, accessToken);
        var ratings = new Dictionary<string, int>();

        if (jsonDoc.RootElement.TryGetProperty("brackets", out var brackets))
        {
            foreach (var bracket in brackets.EnumerateArray())
            {
                if (bracket.TryGetProperty("href", out var hrefElement))
                {
                    var href = hrefElement.GetString();
                    if (href.Contains("shuffle"))
                    {
                        var spec = DetermineSpecFromHref(href);
                        var rating = await GetRatingFromBracketAsync(href, accessToken);
                        ratings[spec] = rating;
                    }
                }
            }
        }

        return ratings;
    }
    private string DetermineSpecFromHref(string href)
    {
        foreach (var spec in SpecMappings.SpecKeywords)
        {
            if (href.Contains(spec.Key, StringComparison.OrdinalIgnoreCase))
                return spec.Value;
        }

        return "Unknown";
    }


    private async Task<int> GetRatingFromBracketAsync(string url, string accessToken)
    {
        var jsonDoc = await FetchJsonAsync(url, accessToken);
        return GetIntProperty(jsonDoc.RootElement, "rating");
    }


    // GET PvP Leaderboard
    //public async Task<ApiResponseModel<PvpLeaderboardModel>> GetPvpLeaderboardAsync(int pvpSeasonId, string pvpBracket)
    //{
    //    var accessToken = await GetAccessTokenAsync();
    //    var apiUrl = $"{_configuration["Blizzard:ApiBaseUrl"]}/data/wow/pvp-season/{pvpSeasonId}/pvp-leaderboard/{pvpBracket}?namespace=dynamic-us&locale=en_US";

    //    try
    //    {

    //        var jsonDoc = await FetchJsonAsync(apiUrl, accessToken);
    //        var leaderboard = new PvpLeaderboardModel
    //        {
    //            SeasonId = pvpSeasonId,
    //            Bracket = pvpBracket
    //        };


    //        if (jsonDoc.RootElement.TryGetProperty("entries", out var entriesArray))
    //        {
    //            foreach (var entry in entriesArray.EnumerateArray())
    //            {
    //                leaderboard.Entries.Add(new LeaderboardEntry
    //                {
    //                    Rank = GetIntProperty(entry, "rank"),
    //                    Player = new Player
    //                    {
    //                        Name = GetStringProperty(entry.GetProperty("character"), "name"),
    //                        Realm = new Realm
    //                        {
    //                            Slug = GetStringProperty(entry.GetProperty("character").GetProperty("realm"), "slug")
    //                        }
    //                    },
    //                    Faction = new Faction
    //                    {
    //                        Type = GetStringProperty(entry.GetProperty("faction"), "type")
    //                    },
    //                    Rating = GetIntProperty(entry, "rating"),
    //                    SeasonMatchStatistics = new SeasonMatchStatistics
    //                    {
    //                        Played = GetIntProperty(entry.GetProperty("season_match_statistics"), "played"),
    //                        Won = GetIntProperty(entry.GetProperty("season_match_statistics"), "won"),
    //                        Lost = GetIntProperty(entry.GetProperty("season_match_statistics"), "lost")
    //                    }
    //                });
    //            }
    //        }

    //        return new ApiResponseModel<PvpLeaderboardModel>
    //        {
    //            Data = leaderboard,
    //            Message = "Success"
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        return new ApiResponseModel<PvpLeaderboardModel>
    //        {
    //            Data = null,
    //            Message = $"Error: {ex.Message}"
    //        };
    //    }

    //}


    // GET Current Season Index



    public async Task<ApiResponseModel<PvpLeaderboardModel>> GetPvpLeaderboardAsync(int pvpSeasonId, string pvpBracket)
    {
        var accessToken = await GetAccessTokenAsync();
        var apiUrl = $"{_configuration["Blizzard:ApiBaseUrl"]}/data/wow/pvp-season/{pvpSeasonId}/pvp-leaderboard/{pvpBracket}?namespace=dynamic-us&locale=en_US";

        try
        {
            var jsonDoc = await FetchJsonAsync(apiUrl, accessToken);
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

            // Ensure that leaderboard.Entries is not null or empty
            if (leaderboard.Entries.Count == 0)
            {
                throw new Exception("No entries found in the leaderboard response.");
            }

            return new ApiResponseModel<PvpLeaderboardModel>
            {
                Data = leaderboard,
                Message = "Success"
            };
        }
        catch (Exception ex)
        {
            // Log the exception for debugging purposes
            Console.WriteLine($"Error fetching leaderboard: {ex.Message}");

            return new ApiResponseModel<PvpLeaderboardModel>
            {
                Data = null,
                Message = $"Error: {ex.Message}"
            };
        }
    }






    public async Task<int> GetCurrentSeasonAsync()
    {
        var accessToken = await GetAccessTokenAsync();
        var apiUrl = $"{_configuration["Blizzard:ApiBaseUrl"]}/data/wow/pvp-season/index?namespace=dynamic-us&locale=en_US";

        try
        {
            var jsonDoc = await FetchJsonAsync(apiUrl, accessToken);

            if (jsonDoc.RootElement.TryGetProperty("current_season", out var currentSeason))
            {
                return GetIntProperty(currentSeason, "id");
            }

            throw new Exception("Current season not found in the response.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error fetching current season: {ex.Message}");
        }
    }





    // Fetch Class Icon URL
    public async Task<string> GetClassIconUrlAsync(int classId)
    {
        var accessToken = await GetAccessTokenAsync();
        var apiUrl = $"{_configuration["Blizzard:ApiBaseUrl"]}/data/wow/media/playable-class/{classId}?namespace=static-us&locale=en_US";

        var jsonDoc = await FetchJsonAsync(apiUrl, accessToken);

        return jsonDoc.RootElement.GetProperty("assets")
            .EnumerateArray()
            .FirstOrDefault(asset => asset.GetProperty("key").GetString() == "icon")
            .GetProperty("value")
            .GetString();
    }

    // Fetch Specialization Icon URL
    public async Task<string> GetSpecializationIconUrlAsync(int specializationId)
    {
        var accessToken = await GetAccessTokenAsync();
        var apiUrl = $"{_configuration["Blizzard:ApiBaseUrl"]}/data/wow/media/playable-specialization/{specializationId}?namespace=static-us&locale=en_US";

        var jsonDoc = await FetchJsonAsync(apiUrl, accessToken);

        return jsonDoc.RootElement.GetProperty("assets")
            .EnumerateArray()
            .FirstOrDefault(asset => asset.GetProperty("key").GetString() == "icon")
            .GetProperty("value")
            .GetString();
    }

    // Fetch All Playable Classes
    public async Task<List<PlayableClass>> GetPlayableClassesAsync()
    {
        var accessToken = await GetAccessTokenAsync();
        var apiUrl = $"{_configuration["Blizzard:ApiBaseUrl"]}/data/wow/playable-class/index?namespace=static-us&locale=en_US";

        var jsonDoc = await FetchJsonAsync(apiUrl, accessToken);

        return jsonDoc.RootElement.GetProperty("classes")
            .EnumerateArray()
            .Select(cls => new PlayableClass
            {
                Id = cls.GetProperty("id").GetInt32(),
                Name = cls.GetProperty("name").GetString(),
                IconUrl = GetClassIconUrlAsync(cls.GetProperty("id").GetInt32()).Result
            }).ToList();
    }

    // Fetch Specializations for a Class
    public async Task<Dictionary<int, string>> GetSpecializationsForClassAsync(int classId)
    {
        var accessToken = await GetAccessTokenAsync();
        var apiUrl = $"{_configuration["Blizzard:ApiBaseUrl"]}/data/wow/playable-class/{classId}?namespace=static-us&locale=en_US";

        var jsonDoc = await FetchJsonAsync(apiUrl, accessToken);

        if (jsonDoc.RootElement.TryGetProperty("specializations", out var specializationsArray))
        {
            return specializationsArray.EnumerateArray()
                .ToDictionary(
                    spec => spec.GetProperty("id").GetInt32(),
                    spec => spec.GetProperty("name").GetString()
                );
        }

        return new Dictionary<int, string>();
    }
}
