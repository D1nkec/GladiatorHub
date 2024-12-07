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
        var specMappings = new Dictionary<string, string>
        {
            { "frost", "Frost" },
            { "fire", "Fire" },
            { "arcane", "Arcane" }
        };

        foreach (var spec in specMappings)
        {
            if (href.Contains(spec.Key))
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

            return new ApiResponseModel<PvpLeaderboardModel>
            {
                Data = leaderboard,
                Message = "Success"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponseModel<PvpLeaderboardModel>
            {
                Data = null,
                Message = $"Error: {ex.Message}"
            };
        }

    }


    // GET Current Season Index
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





  // GET Class Icons
  
    public async Task<string> GetClassIconUrlAsync(int classId)
    {
        var accessToken = await GetAccessTokenAsync();
        var apiUrl = $"{_configuration["Blizzard:ApiBaseUrl"]}/data/wow/media/playable-class/{classId}?namespace=static-us&locale=en_US";

        var jsonDoc = await FetchJsonAsync(apiUrl, accessToken);

        // Dohvaćanje URL-a ikone
        return jsonDoc.RootElement.GetProperty("assets")
            .EnumerateArray()
            .FirstOrDefault(asset => asset.GetProperty("key").GetString() == "icon")
            .GetProperty("value")
            .GetString();
    }

    public async Task<List<PlayableClass>> GetAllClassIconsAsync()
    {
        var classIds = new Dictionary<int, string>
    {
        { 1, "Warrior" },
        { 2, "Paladin" },
        { 3, "Hunter" },
        { 4, "Rogue" },
        { 5, "Priest" },
        { 6, "Death Knight" },
        { 7, "Shaman" },
        { 8, "Mage" },
        { 9, "Warlock" },
        { 10, "Monk" },
        { 11, "Druid" },
        { 12, "Demon Hunter" },
        { 13, "Evoker" }
    };

        var playableClasses = new List<PlayableClass>();

        foreach (var classEntry in classIds)
        {
            var iconUrl = await GetClassIconUrlAsync(classEntry.Key);
            playableClasses.Add(new PlayableClass
            {
                Id = classEntry.Key,
                Name = classEntry.Value,
                IconUrl = iconUrl
            });
        }

        return playableClasses;
    }




    // GET Specialization icons
    public async Task<List<PlayableSpecialization>> GetAllSpecializationIconsAsync()
    {
        var specializationIds = new Dictionary<int, string>
    {
        { 263, "Enhancement" },
        { 270, "Mistweaver" },
        { 66, "Protection" },
        { 255, "Survival" },
        { 256, "Discipline" },
        { 268, "Brewmaster" },
        { 72, "Fury" },
        { 252, "Unholy" },
        { 259, "Assassination" },
        { 264, "Restoration" },
        { 64, "Frost" },
        { 62, "Arcane" },
        { 65, "Holy" },
        { 70, "Retribution" },
        { 71, "Arms" },
        { 73, "Protection" },
        { 253, "Beast Mastery" },
        { 254, "Marksmanship" },
        { 258, "Shadow" },
        { 261, "Subtlety" },
        { 103, "Feral" },
        { 104, "Guardian" },
        { 105, "Restoration" },
        { 267, "Destruction" },
        { 257, "Holy" },
        { 63, "Fire" },
        { 250, "Blood" },
        { 251, "Frost" },
        { 260, "Outlaw" },
        { 262, "Elemental" },
        { 265, "Affliction" },
        { 266, "Demonology" },
        { 269, "Windwalker" },
        { 102, "Balance" },
        { 577, "Havoc" },
        { 581, "Vengeance" },
        { 1467, "Devastation" },
        { 1468, "Preservation" },
        { 1473, "Augmentation" }
    };

        var playableSpecializations = new List<PlayableSpecialization>();

        foreach (var specializationEntry in specializationIds)
        {
            var iconUrl = await GetSpecializationIconUrlAsync(specializationEntry.Key);
            playableSpecializations.Add(new PlayableSpecialization
            {
                Id = specializationEntry.Key,
                Name = specializationEntry.Value,
                IconUrl = iconUrl
            });
        }

        return playableSpecializations;
    }

    public async Task<string> GetSpecializationIconUrlAsync(int specializationId)
    {
        var accessToken = await GetAccessTokenAsync();
        var apiUrl = $"{_configuration["Blizzard:ApiBaseUrl"]}/data/wow/media/playable-specialization/{specializationId}?namespace=static-us&locale=en_US";

        var jsonDoc = await FetchJsonAsync(apiUrl, accessToken);

        // Fetch the URL of the icon
        return jsonDoc.RootElement.GetProperty("assets")
            .EnumerateArray()
            .FirstOrDefault(asset => asset.GetProperty("key").GetString() == "icon")
            .GetProperty("value")
            .GetString();
    }




}


