namespace Steam_API.Dto.Output
{
    public sealed class AchievementDto
    {
        public string ApiName { get; init; } = default!;   // "apiname" in user stats / "name" in schema
        public string? DisplayName { get; init; }
        public string? Description { get; init; }          // may be missing for hidden cheevos until unlocked
        public bool Achieved { get; init; }
        public DateTimeOffset? UnlockTime { get; init; }   // from GetPlayerAchievements
        public string? Icon { get; init; }                 // colored icon URL
        public string? IconGray { get; init; }             // gray icon URL
        public double? GlobalPercent { get; init; }        // optional (see below)
    }
}


