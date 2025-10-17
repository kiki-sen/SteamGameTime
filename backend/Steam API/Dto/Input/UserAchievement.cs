namespace Steam_API.Dto.Input
{
    public sealed class UserAchievement
    {
        public string ApiName { get; set; } = default!;        // "apiname"
        public int Achieved { get; set; }                      // 0/1
        public long? UnlockTimeUnix { get; set; }              // "unlocktime" (seconds)
    }
}


