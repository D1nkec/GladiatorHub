using GladiatorHub.Models.GladiatorHub.Models;
using GladiatorHub.Models;

namespace GladiatorHub.Services.Interface
{
    public interface IGameDataService
    {
        Task<List<PlayableSpecialization>> GetAllSpecializationsAsync();
        Task<string> GetClassIconUrlAsync(int classId);
        string GetFactionIconUrl(string factionType);
        Task<List<PlayableClass>> GetPlayableClassesAsync();
        Task<string> GetSpecializationIconUrlAsync(int specializationId);
        Task<List<PlayableSpecialization>> GetSpecializationsByClassIdAsync(int classId);
        Task<Dictionary<int, string>> GetSpecializationsForClassAsync(int classId);
    }
}
