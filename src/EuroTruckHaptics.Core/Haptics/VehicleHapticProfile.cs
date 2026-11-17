namespace EuroTruckHaptics.Haptics;

public sealed record VehicleHapticProfile
{
    public string Id { get; init; } = "generic";
    public string DisplayName { get; init; } = "Generic truck";
    public double IdleRpm { get; init; } = 550;
    public double RedlineRpm { get; init; } = 2200;
    public float EngineVibration { get; init; } = 0.4f;
    public float HeavyLoadMultiplier { get; init; } = 1.3f;
}
