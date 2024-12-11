
using GladiatorHub.Models.GladiatorHub.Models;

public class PvpLeaderboardModel
{
    public int SeasonId { get; set; }
    public string Bracket { get; set; }
    public List<LeaderboardEntry> Entries { get; set; } = new List<LeaderboardEntry>();
}


public class LeaderboardEntry
{
    public int Rank { get; set; }
    public Player Player { get; set; }
    public Faction Faction { get; set; }
    public int Rating { get; set; }
    public SeasonMatchStatistics SeasonMatchStatistics { get; set; }
}


public class Player
{
   
    public string Name { get; set; }

    public Realm Realm { get; set; }
}


public class Realm
{
    public string Slug { get; set; }  
}


public class Faction
{
    public string Type { get; set; }  
}


public class SeasonMatchStatistics
{
    public int Played { get; set; }
    public int Won { get; set; }
    public int Lost { get; set; }
}
