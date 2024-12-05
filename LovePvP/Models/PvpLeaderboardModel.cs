// PvpLeaderboardModel - Cjelokupni leaderboard
public class PvpLeaderboardModel
{
    public int SeasonId { get; set; }
    public string Bracket { get; set; }
    public List<LeaderboardEntry> Entries { get; set; } = new List<LeaderboardEntry>();
}

// LeaderboardEntry - Pojedinačni unos na leaderboardu
public class LeaderboardEntry
{
    public int Rank { get; set; }
    public Player Player { get; set; }
    public Faction Faction { get; set; }
    public int Rating { get; set; }
    public SeasonMatchStatistics SeasonMatchStatistics { get; set; }
}

// Player - Igrač na leaderboardu
public class Player
{
    public string Name { get; set; }
    public Realm Realm { get; set; }
}

// Realm - Detalji o serveru na kojem igrač igra
public class Realm
{
    public string Slug { get; set; }  // Npr. "tichondrius"
}

// Faction - Faksija igrača (ALLIANCE / HORDE)
public class Faction
{
    public string Type { get; set; }  // "ALLIANCE" ili "HORDE"
}

// SeasonMatchStatistics - Statistika mečeva za sezonu
public class SeasonMatchStatistics
{
    public int Played { get; set; }
    public int Won { get; set; }
    public int Lost { get; set; }
}
