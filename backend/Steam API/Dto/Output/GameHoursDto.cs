namespace Steam_API.Dto.Output
{

    /// <summary>Aggregated playtime information in hours.</summary>
    public class GameHoursDto
    {
        /// <summary>Steam AppID.</summary>
        public int AppId { get; init; }
        /// <summary>Game title.</summary>
        public string? Name { get; init; }
        /// <summary>Total lifetime hours played (rounded 0.1h).</summary>
        public double HoursTotal { get; init; }
        /// <summary>Hours played in the last two weeks (rounded 0.1h).</summary>
        public double Hours2Weeks { get; init; }
        /// <summary>Icon image URL.</summary>
        public string? img_icon_url { get; set; }
        /// <summary>Logo image URL.</summary>
        public string? img_logo_url { get; set; }
    }
}


