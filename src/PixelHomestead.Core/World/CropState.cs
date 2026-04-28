namespace PixelHomestead.Core.World;

public sealed record CropState
{
    public required string CropId { get; init; }
    public int GrowthProgress { get; init; }
    public bool WateredToday { get; init; }
}
