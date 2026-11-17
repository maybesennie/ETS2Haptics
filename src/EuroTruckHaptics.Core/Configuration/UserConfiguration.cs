namespace EuroTruckHaptics.Configuration;

public sealed record UserConfiguration
{
    public int SchemaVersion { get; init; } = 1;
    public Haptics.HapticSettings HapticSettings { get; init; } = new();
    public Haptics.VehicleHapticProfile VehicleProfile { get; init; } = new();
}
