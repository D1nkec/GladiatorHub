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

        // Mapiranje podataka u model
        var pvpSummary = new PvpSummaryModel
        {
            CharacterName = jsonData.RootElement.GetProperty("character").GetProperty("name").GetString(),
            RealmName = jsonData.RootElement.GetProperty("character").GetProperty("realm").GetProperty("name").GetString(),
            HonorLevel = jsonData.RootElement.GetProperty("honor_level").GetInt32(),
            HonorableKills = jsonData.RootElement.GetProperty("honorable_kills").GetInt32(),
            PvpMapStatistics = jsonData.RootElement
                .GetProperty("pvp_map_statistics")
                .EnumerateArray()
                .Select(map => new MapStatistics
                {
                    MapName = map.GetProperty("world_map").GetProperty("name").GetString(),
                    Played = map.GetProperty("match_statistics").GetProperty("played").GetInt32(),
                    Won = map.GetProperty("match_statistics").GetProperty("won").GetInt32(),
                    Lost = map.GetProperty("match_statistics").GetProperty("lost").GetInt32()
                })
                .ToList()
        };

        return pvpSummary;
    }

    public async Task<int> GetSoloShuffleRatingAsync(string realmSlug, string characterName)
    {
        var accessToken = await GetAccessTokenAsync();
        var url = $"{_configuration["Blizzard:ApiBaseUrl"]}/profile/wow/character/{realmSlug}/{characterName}/pvp-summary?namespace=profile-us";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return 0; // Nema podataka

        response.EnsureSuccessStatusCode();
        var jsonString = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(jsonString);

        // Pronađi PvP bracket za Solo Shuffle
        foreach (var bracket in jsonDoc.RootElement.GetProperty("brackets").EnumerateArray())
        {
            var href = bracket.GetProperty("href").GetString();
            if (href.Contains("shuffle"))
            {
                return await GetRatingFromBracketAsync(href, accessToken);
            }
        }
        return 0; // Ako nema Shuffle bracketa
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


}

