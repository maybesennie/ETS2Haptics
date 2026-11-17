namespace EuroTruckHaptics.Haptics;

public sealed class HapticEngine
{
    private const float AbsPulseFrequencyHz = 12f;

    public HapticFrame Process(
        in TelemetrySample sample,
        HapticSettings settings,
        VehicleHapticProfile profile)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);

        var speed = NonNegative(sample.SpeedMetersPerSecond);
        var rpm = NonNegative(sample.EngineRpm);
        var load = Unit(sample.EngineLoad);
        var throttle = Unit(sample.Throttle);
        var brake = Unit(sample.Brake);
        var retarder = Unit(sample.Retarder);

        var rpmRange = Math.Max(1d, profile.RedlineRpm - profile.IdleRpm);
        var rpmFactor = Unit((float)((rpm - profile.IdleRpm) / rpmRange));
        var engine = rpm > 0
            ? Unit(settings.EngineIntensity * profile.EngineVibration *
                   (0.25f + 0.75f * rpmFactor) *
                   (1f + (profile.HeavyLoadMultiplier - 1f) * load))
            : 0f;

        var surface = SurfaceIntensity(sample.Surface);
        var speedFactor = Unit((float)(speed / 30d));
        var road = surface * settings.RoadIntensity * speedFactor;
        var leftSlip = Unit(sample.LeftWheelSlip);
        var rightSlip = Unit(sample.RightWheelSlip);
        var slipFeedback = settings.SlipIntensity;

        var absActive = sample.AbsActive == true;
        var absPulse = absActive &&
            (Math.Sin(sample.Timestamp.ToUnixTimeMilliseconds() / 1000d *
                      Math.Tau * AbsPulseFrequencyHz) >= 0);
        var brakeFeedback = brake * (absPulse ? 1f : absActive ? 0.18f : 0.5f);

        var left = Unit((engine + road + leftSlip * slipFeedback) * settings.Intensity);
        var right = Unit((engine + road + rightSlip * slipFeedback) * settings.Intensity);
        var l2 = Unit((brakeFeedback + retarder * 0.25f) * settings.Intensity);
        var r2 = Unit((throttle * 0.65f + load * 0.35f) * settings.Intensity);

        return new HapticFrame(
            settings.LeftActuatorEnabled ? left : 0f,
            settings.RightActuatorEnabled ? right : 0f,
            settings.L2Enabled ? l2 : 0f,
            settings.R2Enabled ? r2 : 0f,
            absPulse,
            sample.Timestamp);
    }

    private static float SurfaceIntensity(RoadSurface? surface) => surface switch
    {
        RoadSurface.Asphalt => 0.08f,
        RoadSurface.Concrete => 0.14f,
        RoadSurface.Gravel => 0.55f,
        RoadSurface.Dirt => 0.35f,
        RoadSurface.Mud => 0.25f,
        RoadSurface.Cobblestone => 0.75f,
        RoadSurface.Snow => 0.2f,
        RoadSurface.Ice => 0.1f,
        _ => 0f
    };

    private static float Unit(double? value) =>
        value is { } number && double.IsFinite(number)
            ? Unit((float)number)
            : 0f;

    private static float Unit(float value) =>
        float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : 0f;

    private static double NonNegative(double? value) =>
        value is { } number && double.IsFinite(number) ? Math.Max(0d, number) : 0d;
}
