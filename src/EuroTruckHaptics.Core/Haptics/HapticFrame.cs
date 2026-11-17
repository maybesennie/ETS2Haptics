namespace EuroTruckHaptics.Haptics;

public readonly record struct HapticFrame(
    float LeftActuator,
    float RightActuator,
    float L2BrakeResistance,
    float R2ThrottleResistance,
    bool AbsPulse,
    DateTimeOffset Timestamp);
