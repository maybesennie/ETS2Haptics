using EuroTruckHaptics.Haptics;

namespace EuroTruckHaptics.Telemetry;

public enum TelemetryConnectionState
{
    Disconnected,
    Connected,
    GameInactive,
    Stale,
    UnsupportedPlugin,
    InconsistentSnapshot,
    AccessDenied
}

public readonly record struct TelemetryCapability(
    string Name,
    Type DataType,
    bool Available,
    string Source,
    string? Limitation = null,
    string? ValidityRule = null,
    string? FallbackBehavior = null);

public sealed record TelemetryReadResult(
    TelemetryConnectionState State,
    TelemetrySample? Sample,
    string Message,
    double? UpdateFrequencyHz = null);
