using System.Text.Json;

public interface IBlizzardApiService
{
    Task<JsonDocument> FetchJsonAsync(string url, string accessToken);
    Task<string> GetAccessTokenAsync();
    Task<JsonDocument> SafeFetchJsonAsync(string url, string accessToken);
}