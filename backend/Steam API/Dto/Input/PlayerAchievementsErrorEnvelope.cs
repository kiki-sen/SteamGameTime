namespace Steam_API.Dto.Input
{
    public sealed class PlayerAchievementsErrorEnvelope
    {
        public PlayerStatsError? Playerstats { get; set; }  // note: Steam may lowercase/uppercase inconsistently
    }
}


