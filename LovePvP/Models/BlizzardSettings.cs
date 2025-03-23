namespace GladiatorHub.Models
{
    public class BlizzardSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TokenUrl { get; set; }
        public Dictionary<BlizzardRegion, string> ApiBaseUrls { get; set; }

        public enum BlizzardRegion
        {
            US,
            EU
        }
    }
}
