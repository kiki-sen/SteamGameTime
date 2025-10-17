namespace Steam_API.Dto.Output
{
    public sealed class FriendHoursRow
    {
        public string SteamId64 { get; init; } = default!;
        public string PersonaName { get; init; } = "";
        public string AvatarMedium { get; init; } = "";   // 64x64
        public bool IsYou { get; init; }
        public double HoursTotal { get; init; }           
        public double? Hours2Weeks { get; init; }   
        public bool PrivateOrUnavailable { get; init; } // true if profile/stats not accessible
    }
}


