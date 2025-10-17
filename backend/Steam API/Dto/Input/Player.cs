namespace Steam_API.Dto.Input
{
    public class Player
    {
        public string Steamid { get; set; } = default!;
        public string? Personaname { get; set; }
        public string? Avatar { get; set; }
        public string? Avatarmedium { get; set; }
        public string? Avatarfull { get; set; }
        public string? Loccountrycode { get; set; }
        public int Communityvisibilitystate { get; set; }
        public int? Personastate { get; set; }
        public long? Timecreated { get; set; }   // unix seconds
        public long? Lastlogoff { get; set; }    // unix seconds
        public string? Gameid { get; set; }      // currently playing game ID
        public string? Gameextrainfo { get; set; } // currently playing game name
    }
}

