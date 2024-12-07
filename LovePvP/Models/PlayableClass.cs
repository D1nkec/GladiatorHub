using GladiatorHub.Models.GladiatorHub.Models;

namespace GladiatorHub.Models
{
    public class PlayableClass
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string IconUrl { get; set; }

        public List<PlayableSpecialization> Specializations { get; set; }
    }

}
