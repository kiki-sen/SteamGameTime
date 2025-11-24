namespace Steam_API.Dto.Output
{
    public sealed class GameDetailsDto
    {
        public int AppId { get; init; }
        public string? Name { get; init; }
        public string? HeaderImage { get; init; }     // from store appdetails
        public string? ShortDescription { get; init; }
        public string? DetailedDescription { get; init; }
        public string? AboutTheGame { get; init; }
        public string? Website { get; init; }
        public string?[]? Developers { get; init; }
        public string?[]? Publishers { get; init; }
        public string?[]? Genres { get; init; }
        public int? CurrentPlayers { get; init; }
        public IReadOnlyList<AchievementDto> Achievements { get; init; } = [];
        public PlatformsDto? Platforms { get; set; }
        public bool SupportsLinux => Platforms?.linux == true;
    }
}


