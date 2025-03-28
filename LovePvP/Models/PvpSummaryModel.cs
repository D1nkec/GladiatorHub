﻿namespace GladiatorHub.Models
{
    public class PvpSummaryModel
    {
        public string CharacterName { get; set; }
        public string RealmName { get; set; }
        public int HonorLevel { get; set; }
        public int HonorableKills { get; set; }
        public List<MapStatistics> PvpMapStatistics { get; set; }
        public Dictionary<string, int> SoloShuffleRating { get; set; }
        public Dictionary<string, int> BgBlitzRating { get; set; }
        public Dictionary<string, int> ArenaRating { get; set; }
    }

}
