namespace EuroTruckHaptics.Haptics;

public enum RoadSurface
{
    Unknown,
    Asphalt,
    Concrete,
    Gravel,
    Dirt,
    Mud,
    Cobblestone,
    Snow,
    Ice
}

public readonly record struct TelemetrySample(
    DateTimeOffset Timestamp,
    double? SpeedMetersPerSecond = null,
    double? EngineRpm = null,
    double? EngineLoad = null,
    double? Throttle = null,
    double? Brake = null,
    double? Retarder = null,
    double? LeftWheelSlip = null,
    double? RightWheelSlip = null,
    double? VerticalAcceleration = null,
    bool? AbsActive = null,
    RoadSurface? Surface = null,
    int? Gear = null,
    string? TruckBrand = null,
    string? TruckName = null);
