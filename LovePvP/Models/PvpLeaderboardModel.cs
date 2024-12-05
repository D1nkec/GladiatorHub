namespace GladiatorHub.Models
{
    public class PvpLeaderboardModel
    {
        public string PlayerName { get; set; }
        public int Rating { get; set; }
    }
    public class PvpLeaderboardViewModel
    {
        public List<PvpLeaderboardModel> Leaderboard { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string PvpBracket { get; set; }
        public int PvpSeasonId { get; set; }
    }
}
