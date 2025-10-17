namespace Steam_API.Dto.Output
{
    /// <summary>Basic game metadata returned to the client.</summary>
    public class GameDto
    {
        /// <summary>Steam AppID.</summary>
        public int appid { get; set; }
        /// <summary>Localized title.</summary>
        public string name { get; set; } = string.Empty;
        /// <summary>Icon hash used to build an image URL.</summary>
        public string? img_icon_url { get; set; }
        /// <summary>Total playtime in minutes.</summary>
        public int playtime_forever { get; set; }
    }

}


