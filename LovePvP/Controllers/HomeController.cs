using GladiatorHub.Models.GladiatorHub.Models;
using GladiatorHub.Models;
using GladiatorHub.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using GladiatorHub.Services.Implementation;

public class HomeController : Controller
{
    private readonly IGameDataService _gameDataService;
    private readonly ILeaderboardService _leaderboardService;
    private readonly ILogger<HomeController> _logger;
    private readonly IMemoryCache _cache;
   

    public HomeController(IGameDataService gameDataService, ILeaderboardService leaderboardService, ILogger<HomeController> logger, IMemoryCache cache)
    {
        _gameDataService = gameDataService;
        _leaderboardService = leaderboardService;
        _logger = logger;
        _cache = cache;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            List<PlayableClass> playableClasses;
            if (!_cache.TryGetValue("PlayableClasses", out playableClasses))
            {
                playableClasses = await _gameDataService.GetPlayableClassesAsync();
                _cache.Set("PlayableClasses", playableClasses, TimeSpan.FromHours(1));
            }

            var specializationTasks = playableClasses.Select(async playableClass =>
            {
                var specializations = await _gameDataService.GetSpecializationsForClassAsync(playableClass.Id);
                var tasks = specializations.Select(async spec =>
                {
                    var iconUrl = await _gameDataService.GetSpecializationIconUrlAsync(spec.Key);
                    return new PlayableSpecialization
                    {
                        Id = spec.Key,
                        Name = spec.Value,
                        IconUrl = iconUrl
                    };
                });
                return new
                {
                    ClassId = playableClass.Id,
                    Specializations = await Task.WhenAll(tasks)
                };
            }).ToArray();

            var results = await Task.WhenAll(specializationTasks);
            var classSpecializations = results.ToDictionary(x => x.ClassId, x => x.Specializations.ToList());

           
            var (leaderboard2v2, totalEntries2v2) = await GetLeaderboardData("2v2", 1);
            var (leaderboard3v3, totalEntries3v3) = await GetLeaderboardData("3v3", 1);
            var (leaderboardRbg, totalEntriesRbg) = await GetLeaderboardData("rbg", 1);

            int PageSize = 10;

          
            ViewBag.ClassSpecializations = classSpecializations;
            ViewBag.Leaderboard2v2 = leaderboard2v2;
            ViewBag.Leaderboard3v3 = leaderboard3v3;
            ViewBag.LeaderBoardRbg = leaderboardRbg;
            ViewBag.TotalPages2v2 = (int)Math.Ceiling((double)totalEntries2v2 / PageSize);
            ViewBag.TotalPages3v3 = (int)Math.Ceiling((double)totalEntries3v3 / PageSize);
            ViewBag.TotalPagesRbg = (int)Math.Ceiling((double)totalEntriesRbg / PageSize);


            return View(playableClasses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching playable classes and leaderboards.");
            ViewBag.ErrorMessage = "An error occurred while fetching the data. Please try again later.";
            return View();
        }
    }

    private async Task<(List<LeaderboardEntry> Entries, int TotalEntries)> GetLeaderboardData(string gameMode, int page)
    {
        try
        {
            var currentSeason = await _leaderboardService.GetCurrentSeasonAsync();
            var apiResponse = await _leaderboardService.GetPvpLeaderboardAsync(currentSeason, gameMode);
            var leaderboard = apiResponse?.Data;

            if (leaderboard == null || leaderboard.Entries == null || leaderboard.Entries.Count == 0)
            {
                _logger.LogWarning($"No leaderboard data found for {gameMode}.");
                return (new List<LeaderboardEntry>(), 0);
            }

            // Get total number of entries before pagination
            int totalEntries = leaderboard.Entries.Count;

            // Apply pagination
            int PageSize = 10;
            var paginatedEntries = leaderboard.Entries
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Assign faction icons dynamically
            foreach (var entry in paginatedEntries)
            {
                entry.Faction.IconUrl = _gameDataService.GetFactionIconUrl(entry.Faction.Type);
            }

            return (paginatedEntries, totalEntries);
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, $"Error fetching {gameMode} leaderboard data from Blizzard API.");
            return (new List<LeaderboardEntry>(), 0);
        }
        catch (TimeoutException timeoutEx)
        {
            _logger.LogError(timeoutEx, $"Request timed out for {gameMode} leaderboard.");
            return (new List<LeaderboardEntry>(), 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An unexpected error occurred while fetching {gameMode} leaderboard.");
            return (new List<LeaderboardEntry>(), 0);
        }
    }

    [HttpGet]
    public async Task<IActionResult> FetchLeaderboardDataAsync(string gameMode, int page = 1)
    {
        int pageSize = 10;
        var (leaderboardEntries, totalEntries) = await GetLeaderboardData(gameMode, page);

        
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalEntries / pageSize);
        ViewBag.GameMode = gameMode;

        return PartialView("_LeaderboardPartial", leaderboardEntries);
    }





}
