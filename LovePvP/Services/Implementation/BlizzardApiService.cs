using GladiatorHub.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;


public class BlizzardApiService : IBlizzardApiService
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
    public async Task<JsonDocument> FetchJsonAsync(string url, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(jsonString);
    }

    // Pomoćna metoda za sigurno dohvaćanje JSON odgovora s obradom grešaka
    public async Task<JsonDocument> SafeFetchJsonAsync(string url, string accessToken)
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

    
    
}
