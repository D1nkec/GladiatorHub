using GladiatorHub.Models.GladiatorHub.Models;
using GladiatorHub.Models;
using GladiatorHub.Services.Interface;

namespace GladiatorHub.Services.Implementation
{
    public class GameDataService : IGameDataService
    {
        private readonly IBlizzardApiService _blizzardApiService;
        public GameDataService(IBlizzardApiService blizzardApiService)
        {
            _blizzardApiService = blizzardApiService;
        }

        public async Task<string> GetClassIconUrlAsync(int classId)
        {
            var accessToken = await _blizzardApiService.GetAccessTokenAsync();
            var apiUrl = $"https://us.api.blizzard.com/data/wow/media/playable-class/{classId}?namespace=static-us&locale=en_US";

            var jsonDoc = await _blizzardApiService.FetchJsonAsync(apiUrl, accessToken);

            return jsonDoc.RootElement.GetProperty("assets")
                .EnumerateArray()
                .FirstOrDefault(asset => asset.GetProperty("key").GetString() == "icon")
                .GetProperty("value")
                .GetString();
        }
        public async Task<string> GetSpecializationIconUrlAsync(int specializationId)
        {
            var accessToken = await _blizzardApiService.GetAccessTokenAsync();
            var apiUrl = $"https://us.api.blizzard.com/data/wow/media/playable-specialization/{specializationId}?namespace=static-us&locale=en_US";

            var jsonDoc = await _blizzardApiService.FetchJsonAsync(apiUrl, accessToken);

            return jsonDoc.RootElement.GetProperty("assets")
                .EnumerateArray()
                .FirstOrDefault(asset => asset.GetProperty("key").GetString() == "icon")
                .GetProperty("value")
                .GetString();
        }
        public async Task<List<PlayableClass>> GetPlayableClassesAsync()
        {
            var accessToken = await _blizzardApiService.GetAccessTokenAsync();
            var apiUrl = $"https://us.api.blizzard.com/data/wow/playable-class/index?namespace=static-us&locale=en_US";

            var jsonDoc = await _blizzardApiService.FetchJsonAsync(apiUrl, accessToken);

            return jsonDoc.RootElement.GetProperty("classes")
                .EnumerateArray()
                .Select(cls => new PlayableClass
                {
                    Id = cls.GetProperty("id").GetInt32(),
                    Name = cls.GetProperty("name").GetString(),
                    IconUrl = GetClassIconUrlAsync(cls.GetProperty("id").GetInt32()).Result
                }).ToList();
        }
        public async Task<Dictionary<int, string>> GetSpecializationsForClassAsync(int classId)
        {
            var accessToken = await _blizzardApiService.GetAccessTokenAsync();
            var apiUrl = $"https://us.api.blizzard.com/data/wow/playable-class/{classId}?namespace=static-us&locale=en_US";

            var jsonDoc = await _blizzardApiService.FetchJsonAsync(apiUrl, accessToken);

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
        public async Task<List<PlayableSpecialization>> GetAllSpecializationsAsync()
        {
            var accessToken = await _blizzardApiService.GetAccessTokenAsync();
            var apiUrl = $"https://us.api.blizzard.com/data/wow/playable-specialization/index?namespace=static-us&locale=en_US";

            var jsonDoc = await _blizzardApiService.FetchJsonAsync(apiUrl, accessToken);

            return jsonDoc.RootElement.GetProperty("specializations")
                .EnumerateArray()
                .Select(spec => new PlayableSpecialization
                {
                    Id = spec.GetProperty("id").GetInt32(),
                    Name = spec.GetProperty("name").GetString(),

                    IconUrl = GetSpecializationIconUrlAsync(spec.GetProperty("id").GetInt32()).Result
                }).ToList();
        }
        public async Task<List<PlayableSpecialization>> GetSpecializationsByClassIdAsync(int classId)
        {
            var accessToken = await _blizzardApiService.GetAccessTokenAsync();
            var apiUrl = $"https://us.api.blizzard.com/data/wow/playable-class/{classId}?namespace=static-us&locale=en_US";

            var jsonDoc = await _blizzardApiService.FetchJsonAsync(apiUrl, accessToken);

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
                "horde" => "/assets/horde.png",      // Correct URL for Horde image
                "alliance" => "/assets/alliance.png", // Correct URL for Alliance image
                _ => "/images/unknown-icon.png" // Fallback for unknown factions
            };
        }



    }
}
