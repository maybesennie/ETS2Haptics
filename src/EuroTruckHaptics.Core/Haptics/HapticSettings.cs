namespace EuroTruckHaptics.Haptics;

public sealed record HapticSettings
{
    public bool LeftActuatorEnabled { get; init; } = true;
    public bool RightActuatorEnabled { get; init; } = true;
    public bool L2Enabled { get; init; } = true;
    public bool R2Enabled { get; init; } = true;
    public float Intensity { get; init; } = 0.7f;
    public float EngineIntensity { get; init; } = 0.35f;
    public float RoadIntensity { get; init; } = 0.4f;
    public float SlipIntensity { get; init; } = 0.65f;
}
