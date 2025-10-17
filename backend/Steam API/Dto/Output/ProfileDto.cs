namespace Steam_API.Dto.Output
{
    /// <summary>Minimal Steam profile information.</summary>
    public sealed class ProfileDto
    {
        public string SteamId64 { get; init; } = default!;
        public string PersonaName { get; init; } = "";
        public int? SteamLevel { get; init; }
        public string? AvatarSmall { get; init; }     // 32x32
        public string? AvatarMedium { get; init; }    // 64x64
        public string? AvatarFull { get; init; }      // 184x184
        public string? CountryCode { get; init; }     // e.g., "US"
        public int CommunityVisibilityState { get; init; } // 1=Private, 3=Public
        public int? PersonaState { get; init; }       // 0=Offline.. etc.
        public DateTimeOffset? TimeCreatedUtc { get; init; }
        public DateTimeOffset? LastLogOffUtc { get; init; }
    }
}


