namespace GladiatorHub.Mappings
{
    public static class ClassSpecializationMappings
    {
        public static readonly Dictionary<int, Dictionary<int, string>> ClassSpecializations = new()
        {
            { 1, new Dictionary<int, string> { { 71, "Arms" }, { 72, "Fury" }, { 73, "Protection" } } },
            { 2, new Dictionary<int, string> { { 70, "Retribution" },{65,"Holy" }, { 66, "Protection" } } }, 
            { 3, new Dictionary<int, string> { { 253, "BeastMastery" }, { 254, "Marksmanship" }, { 255, "Survival" } } }, 
            { 4, new Dictionary<int, string> { { 259, "Assassination" }, { 261, "Subtlety" }, { 260, "Outlaw" } } }, 
            { 5, new Dictionary<int, string> { { 256, "Discipline" }, { 258, "Shadow" }, { 257, "Holy" } } }, 
            { 6, new Dictionary<int, string> { { 250, "Blood" }, { 251, "Frost" }, { 252, "Unholy" } } }, 
            { 7, new Dictionary<int, string> { { 262, "Elemental" }, { 263, "Enhancement" }, { 264, "Restoration" } } }, 
            { 8, new Dictionary<int, string> { { 62, "Arcane" }, { 63, "Fire" }, { 64, "Frost" } } }, 
            { 9, new Dictionary<int, string> { { 267, "Destruction" }, { 265, "Affliction" }, { 266, "Demonology" } } }, 
            { 10, new Dictionary<int, string> { { 268, "Brewmaster" }, { 270, "Mistweaver" }, { 269, "Windwalker" } } }, 
            { 11, new Dictionary<int, string> { { 103, "Feral" }, { 104, "Guardian" }, { 105, "Restoration" } } }, 
            { 12, new Dictionary<int, string> { { 577, "Havoc" }, { 581, "Vengeance" } } }, 
            { 13, new Dictionary<int, string> { { 1467, "Devastation" }, { 1468, "Preservation" }, { 1473, "Augmentation" } } } 
        };
    }
}
