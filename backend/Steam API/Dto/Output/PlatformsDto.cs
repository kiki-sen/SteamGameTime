namespace Steam_API.Dto.Output
{
    public sealed class PlatformsDto
    {
        public int appId { get; set; }
        public bool windows { get; init; }
        public bool mac { get; init; }
        public bool linux { get; init; }
    }
}
