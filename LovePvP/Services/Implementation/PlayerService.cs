using GladiatorHub.Mappings;
using GladiatorHub.Models;
using System.Text.Json;

public class PlayerService :  IPlayerService
{
    private readonly IBlizzardApiService _blizzardApiService;
    private readonly ILogger<PlayerService> _logger;

    public PlayerService(IBlizzardApiService blizzardApiService, ILogger<PlayerService> logger)
    {
        _blizzardApiService = blizzardApiService;
        _logger = logger;
    }

    //PvP Summary for a character
    public async Task<PvpSummaryModel> GetCharacterPvpSummaryAsync(string realmSlug, string characterName)
    {
        try
        {
            var accessToken = await _blizzardApiService.GetAccessTokenAsync();
            var apiUrl = $"https://us.api.blizzard.com/profile/wow/character/{realmSlug}/{characterName}/pvp-summary?namespace=profile-us&locale=en_US";

            var jsonData = await _blizzardApiService.FetchJsonAsync(apiUrl, accessToken);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching PvP summary for player.");
            throw new Exception("Failed to fetch character PvP summary.", ex);
        }
    }

    //Solo Shuffle Rating for a character
    public async Task<Dictionary<string, int>> GetSoloShuffleRatingAsync(string realmSlug, string characterName)
    {
        var accessToken = await _blizzardApiService.GetAccessTokenAsync();
        var apiUrl = $"https://us.api.blizzard.com/profile/wow/character/{realmSlug}/{characterName}/pvp-summary?namespace=profile-us";

        var jsonDoc = await _blizzardApiService.SafeFetchJsonAsync(apiUrl, accessToken);
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
    private async Task<int> GetRatingFromBracketAsync(string url, string accessToken)
    {
        var jsonDoc = await _blizzardApiService.SafeFetchJsonAsync(url, accessToken);
        return GetIntProperty(jsonDoc.RootElement, "rating");
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


    // 2v2 - 3v3 - rbg Ratings for character
    public async Task<Dictionary<string, int>> GetArenaRatingAsync(string realmSlug, string characterName)
    {
        var accessToken = await _blizzardApiService.GetAccessTokenAsync();
        var apiUrl = $"https://us.api.blizzard.com/profile/wow/character/{realmSlug}/{characterName}/pvp-summary?namespace=profile-us";

        var jsonDoc = await _blizzardApiService.FetchJsonAsync(apiUrl, accessToken);
        var ratings = new Dictionary<string, int>();

        if (jsonDoc.RootElement.TryGetProperty("brackets", out var brackets))
        {
            foreach (var bracket in brackets.EnumerateArray())
            {
                if (bracket.TryGetProperty("href", out var hrefElement))
                {
                    var href = hrefElement.GetString();
                    if (href.Contains("/2v2") || href.Contains("/3v3"))
                    {
                        var bracketType = href.Contains("/2v2") ? "2v2" : "3v3";
                        var rating = await GetRatingFromBracketAsync(href, accessToken);
                        ratings[bracketType] = rating;
                    }
                }
            }
        }

        return ratings;
    }

    // Get BG Blitz rating
    public async Task<Dictionary<string, int>> GetBgBlitzRatingAsync(string realmSlug, string characterName)
    {
        var accessToken = await _blizzardApiService.GetAccessTokenAsync();
        var apiUrl = $"https://us.api.blizzard.com/profile/wow/character/{realmSlug}/{characterName}/pvp-summary?namespace=profile-us";

        var jsonDoc = await _blizzardApiService.FetchJsonAsync(apiUrl, accessToken);
        var ratings = new Dictionary<string, int>();

        if (jsonDoc.RootElement.TryGetProperty("brackets", out var brackets))
        {
            foreach (var bracket in brackets.EnumerateArray())
            {
                if (bracket.TryGetProperty("href", out var hrefElement))
                {
                    var href = hrefElement.GetString();
                    if (href.Contains("blitz"))
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


    // Helper methods
    private string GetStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : string.Empty;
    }

    private int GetIntProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetInt32() : 0;
    }
}
