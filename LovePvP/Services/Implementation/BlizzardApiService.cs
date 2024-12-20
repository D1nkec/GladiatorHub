using GladiatorHub.Mappings;
using GladiatorHub.Models;
using GladiatorHub.Models.GladiatorHub.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;



public class BlizzardApiService
{

    private readonly IConfiguration _configuration;
    private readonly BlizzardSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<BlizzardApiService> _logger;
    private string _accessToken;
    private DateTime _tokenExpiration;

    public BlizzardApiService(IHttpClientFactory httpClientFactory, IOptions<BlizzardSettings> settings, IConfiguration configuration, ILogger<BlizzardApiService> logger)
    {
        _settings = settings.Value;
        _httpClient = httpClientFactory.CreateClient();
        _configuration = configuration;
        _logger = logger;
    }

    // Dohvaćanje pristupnog tokena
    public async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiration)
            return _accessToken;

        var request = new HttpRequestMessage(HttpMethod.Post, _settings.TokenUrl);
        var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);

        _accessToken = jsonDocument.RootElement.GetProperty("access_token").GetString();
        var expiresIn = jsonDocument.RootElement.GetProperty("expires_in").GetInt32();
        _tokenExpiration = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Buffer od 60 sekundi

        return _accessToken;
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

    // Pomoćna metoda za sigurno dohvaćanje JSON odgovora s obradom grešaka
    private async Task<JsonDocument> SafeFetchJsonAsync(string url, string accessToken)
    {
        try
        {
            var jsonDoc = await FetchJsonAsync(url, accessToken);
            return jsonDoc;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP Error occurred while fetching data from Blizzard API.");
            throw new Exception("Blizzard API request failed.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred.");
            throw;
        }
    }

    // GET PvP Summary
    public async Task<PvpSummaryModel> GetCharacterPvpSummaryAsync(string realmSlug, string characterName)
    {
        var accessToken = await GetAccessTokenAsync();
        var apiUrl = $"{_settings.ApiBaseUrl}/profile/wow/character/{realmSlug}/{characterName}/pvp-summary?namespace=profile-us&locale=en_US";

        var jsonData = await SafeFetchJsonAsync(apiUrl, accessToken);

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
        var apiUrl = $"{_settings.ApiBaseUrl}/profile/wow/character/{realmSlug}/{characterName}/pvp-summary?namespace=profile-us";

        var jsonDoc = await SafeFetchJsonAsync(apiUrl, accessToken);
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
        var jsonDoc = await SafeFetchJsonAsync(url, accessToken);
        return GetIntProperty(jsonDoc.RootElement, "rating");
    }

    // GET PvP Leaderboard
    public async Task<ApiResponseModel<PvpLeaderboardModel>> GetPvpLeaderboardAsync(int pvpSeasonId, string pvpBracket)
    {
        var accessToken = await GetAccessTokenAsync();
        var apiUrl = $"{_settings.ApiBaseUrl}/data/wow/pvp-season/{pvpSeasonId}/pvp-leaderboard/{pvpBracket}?namespace=dynamic-us&locale=en_US";

        try
        {
            var jsonDoc = await SafeFetchJsonAsync(apiUrl, accessToken);
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

    // GET Current Season Index
    public async Task<int> GetCurrentSeasonAsync()
    {
        var accessToken = await GetAccessTokenAsync();
        var apiUrl = $"{_settings.ApiBaseUrl}/data/wow/pvp-season/index?namespace=dynamic-us&locale=en_US";

        try
        {
            var jsonDoc = await SafeFetchJsonAsync(apiUrl, accessToken);

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

    // Fetch All Specializations
    public async Task<List<PlayableSpecialization>> GetAllSpecializationsAsync()
    {
        var accessToken = await GetAccessTokenAsync();
        var apiUrl = $"{_configuration["Blizzard:ApiBaseUrl"]}/data/wow/playable-specialization/index?namespace=static-us&locale=en_US";

        var jsonDoc = await FetchJsonAsync(apiUrl, accessToken);

        return jsonDoc.RootElement.GetProperty("specializations")
            .EnumerateArray()
            .Select(spec => new PlayableSpecialization
            {
                Id = spec.GetProperty("id").GetInt32(),
                Name = spec.GetProperty("name").GetString(),
               
                IconUrl = GetSpecializationIconUrlAsync(spec.GetProperty("id").GetInt32()).Result
            }).ToList();
    }

    // Fetch Specializations for a given class by classId
    public async Task<List<PlayableSpecialization>> GetSpecializationsByClassIdAsync(int classId)
    {
        var accessToken = await GetAccessTokenAsync();
        var apiUrl = $"{_configuration["Blizzard:ApiBaseUrl"]}/data/wow/playable-class/{classId}?namespace=static-us&locale=en_US";

        var jsonDoc = await FetchJsonAsync(apiUrl, accessToken);

        return jsonDoc.RootElement.GetProperty("specializations")
            .EnumerateArray()
            .Select(spec => new PlayableSpecialization
            {
                Id = spec.GetProperty("id").GetInt32(),
                Name = spec.GetProperty("name").GetString(),
               
                IconUrl = GetSpecializationIconUrlAsync(spec.GetProperty("id").GetInt32()).Result
            }).ToList();
    }
    public string GetFactionIconUrl(string factionType)
    {
        return factionType.ToLower() switch
        {
            "horde" => "https://wowpedia.fandom.com/wiki/Category:Faction_icons?file=Stormwind.png",
            "alliance" => "https://render.worldofwarcraft.com/icons/alliance-icon.png",
            _ => "/images/unknown-icon.png" // Fallback for unknown factions
        };
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
