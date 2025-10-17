namespace Steam_API.Dto.Input
{
    public sealed class SchemaAchievement
    {
        public string Name { get; set; } = default!;           // API name
        public string? DisplayName { get; set; }
        public string? Description { get; set; }               // hidden achievements: often null until unlocked
        public string? Icon { get; set; }                      // colored
        public string? IconGray { get; set; }                  // gray
    }
}


