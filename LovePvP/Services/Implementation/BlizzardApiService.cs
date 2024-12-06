using LovePvP.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using LovePvP.Models;


public class BlizzardApiService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public BlizzardApiService(IConfiguration configuration)
    {
        _configuration = configuration;
        _httpClient = new HttpClient();
    }

    // Metoda za dohvaćanje pristupnog tokena
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

    // Metoda za dohvaćanje PvP sažetka




    public async Task<PvpSummaryModel> GetCharacterPvpSummaryAsync(string realmSlug, string characterName)
    {
        var accessToken = await GetAccessTokenAsync();
        var apiUrl = $"{_configuration["Blizzard:ApiBaseUrl"]}/profile/wow/character/{realmSlug}/{characterName}/pvp-summary?namespace=profile-us&locale=en_US";

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var jsonData = JsonDocument.Parse(content);

        // Mapiranje podataka u model s provjerom na prisutnost ključeva
        var pvpSummary = new PvpSummaryModel
        {
            // Using TryGetProperty to avoid exceptions if keys are missing
            CharacterName = jsonData.RootElement
                .GetProperty("character").TryGetProperty("name", out var characterNameProp) ? characterNameProp.GetString() : "Unknown",
            RealmName = jsonData.RootElement
                .GetProperty("character").TryGetProperty("realm", out var realmProp) ? realmProp.GetProperty("name").GetString() : "Unknown",
            HonorLevel = jsonData.RootElement
                .TryGetProperty("honor_level", out var honorLevelProp) ? honorLevelProp.GetInt32() : 0,
            HonorableKills = jsonData.RootElement
                .TryGetProperty("honorable_kills", out var honorableKillsProp) ? honorableKillsProp.GetInt32() : 0,
            PvpMapStatistics = jsonData.RootElement
                .TryGetProperty("pvp_map_statistics", out var mapStatsProp) && mapStatsProp.ValueKind == JsonValueKind.Array
                ? mapStatsProp.EnumerateArray().Select(map => new MapStatistics
                {
                    MapName = map.TryGetProperty("world_map", out var worldMapProp) ? worldMapProp.GetProperty("name").GetString() : "Unknown",
                    Played = map.GetProperty("match_statistics").TryGetProperty("played", out var playedProp) ? playedProp.GetInt32() : 0,
                    Won = map.GetProperty("match_statistics").TryGetProperty("won", out var wonProp) ? wonProp.GetInt32() : 0,
                    Lost = map.GetProperty("match_statistics").TryGetProperty("lost", out var lostProp) ? lostProp.GetInt32() : 0
                }).ToList()
                : new List<MapStatistics>()
        };

        return pvpSummary;
    }


    public async Task<Dictionary<string, int>> GetSoloShuffleRatingAsync(string realmSlug, string characterName)
    {
        var accessToken = await GetAccessTokenAsync();
        var url = $"{_configuration["Blizzard:ApiBaseUrl"]}/profile/wow/character/{realmSlug}/{characterName}/pvp-summary?namespace=profile-us";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return new Dictionary<string, int>(); // Nema podataka

        response.EnsureSuccessStatusCode();
        var jsonString = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(jsonString);

        // Pronađi PvP brackets za Solo Shuffle specijalizacije
        var ratings = new Dictionary<string, int>();
        foreach (var bracket in jsonDoc.RootElement.GetProperty("brackets").EnumerateArray())
        {
            var href = bracket.GetProperty("href").GetString();
            if (href.Contains("shuffle"))
            {
                var spec = DetermineSpecFromHref(href); // Ekstrahiraj spec iz URL-a
                var rating = await GetRatingFromBracketAsync(href, accessToken);
                ratings[spec] = rating;
            }
        }
        return ratings; // Vraćamo sve rejtinge
    }

    private string DetermineSpecFromHref(string href)
    {
        if (href.Contains("frost"))
            return "Frost";
        if (href.Contains("fire"))
            return "Fire";
        if (href.Contains("arcane"))
            return "Arcane";
        return "Unknown";
    }


    private async Task<int> GetRatingFromBracketAsync(string url, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(jsonString);

        // Vraćamo Solo Shuffle rating
        return jsonDoc.RootElement.GetProperty("rating").GetInt32();
    }

    public async Task<PvpLeaderboardModel> GetPvpLeaderboardAsync(int pvpSeasonId, string pvpBracket)
    {
        var accessToken = await GetAccessTokenAsync();
        var url = $"{_configuration["Blizzard:ApiBaseUrl"]}/data/wow/pvp-season/{pvpSeasonId}/pvp-leaderboard/{pvpBracket}?namespace=dynamic-us&locale=en_US";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(jsonString);

        var leaderboard = new PvpLeaderboardModel
        {
            SeasonId = pvpSeasonId,
            Bracket = pvpBracket
        };

        if (jsonDoc.RootElement.TryGetProperty("entries", out var entriesArray))
        {
            foreach (var entry in entriesArray.EnumerateArray())
            {
                var leaderboardEntry = new LeaderboardEntry
                {
                    Rank = entry.GetProperty("rank").GetInt32(),
                    Player = new Player
                    {
                        Name = entry.GetProperty("character").GetProperty("name").GetString(),
                        Realm = new Realm
                        {
                            Slug = entry.GetProperty("character").GetProperty("realm").GetProperty("slug").GetString()
                        }
                    },
                    Faction = new Faction
                    {
                        Type = entry.GetProperty("faction").GetProperty("type").GetString()
                    },
                    Rating = entry.GetProperty("rating").GetInt32(),
                    SeasonMatchStatistics = new SeasonMatchStatistics
                    {
                        Played = entry.GetProperty("season_match_statistics").GetProperty("played").GetInt32(),
                        Won = entry.GetProperty("season_match_statistics").GetProperty("won").GetInt32(),
                        Lost = entry.GetProperty("season_match_statistics").GetProperty("lost").GetInt32()
                    }
                };

                leaderboard.Entries.Add(leaderboardEntry);
            }
        }

        return leaderboard;
    }



}

