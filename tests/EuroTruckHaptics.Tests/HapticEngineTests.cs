using EuroTruckHaptics.Haptics;
using EuroTruckHaptics.Configuration;

namespace EuroTruckHaptics.Tests;

[TestClass]
public sealed class HapticEngineTests
{
    private readonly HapticEngine _engine = new();
    private readonly VehicleHapticProfile _profile = new();

    [TestMethod]
    public void Process_KeepsActuatorsAndTriggersIndependent()
    {
        var sample = new TelemetrySample(
            DateTimeOffset.UnixEpoch,
            Throttle: 0.8,
            Brake: 0.6,
            LeftWheelSlip: 0.2,
            RightWheelSlip: 0.8);
        var settings = new HapticSettings
        {
            Intensity = 1f,
            EngineIntensity = 0f,
            RoadIntensity = 0f,
            SlipIntensity = 1f,
            LeftActuatorEnabled = true,
            RightActuatorEnabled = false,
            L2Enabled = false,
            R2Enabled = true
        };

        var frame = _engine.Process(sample, settings, _profile);

        Assert.AreEqual(0.2f, frame.LeftActuator, 0.001f);
        Assert.AreEqual(0f, frame.RightActuator);
        Assert.AreEqual(0f, frame.L2BrakeResistance);
        Assert.AreEqual(0.52f, frame.R2ThrottleResistance, 0.001f);
    }

    [TestMethod]
    public void Process_ProducesDifferentRoadResponses()
    {
        var settings = new HapticSettings
        {
            Intensity = 1f,
            EngineIntensity = 0f,
            RoadIntensity = 1f
        };
        var time = DateTimeOffset.UnixEpoch;

        var asphalt = _engine.Process(
            new TelemetrySample(time, SpeedMetersPerSecond: 20, Surface: RoadSurface.Asphalt),
            settings,
            _profile);
        var gravel = _engine.Process(
            new TelemetrySample(time, SpeedMetersPerSecond: 20, Surface: RoadSurface.Gravel),
            settings,
            _profile);

        Assert.IsGreaterThan(asphalt.LeftActuator, gravel.LeftActuator);
        Assert.AreEqual(gravel.LeftActuator, gravel.RightActuator);
    }

    [TestMethod]
    public void Process_DoesNotInventValuesForMissingTelemetry()
    {
        var frame = _engine.Process(
            new TelemetrySample(DateTimeOffset.UnixEpoch),
            new HapticSettings { Intensity = 1f },
            _profile);

        Assert.AreEqual(0f, frame.LeftActuator);
        Assert.AreEqual(0f, frame.RightActuator);
        Assert.AreEqual(0f, frame.L2BrakeResistance);
        Assert.AreEqual(0f, frame.R2ThrottleResistance);
        Assert.IsFalse(frame.AbsPulse);
    }

    [TestMethod]
    public void Process_ClampsTelemetryAndPulsesAbsOnlyWhenActive()
    {
        var settings = new HapticSettings { Intensity = 1f };
        var inactive = _engine.Process(
            new TelemetrySample(DateTimeOffset.UnixEpoch, Throttle: 2, Brake: 2, AbsActive: false),
            settings,
            _profile);
        var active = _engine.Process(
            new TelemetrySample(DateTimeOffset.UnixEpoch, Brake: 1, AbsActive: true),
            settings,
            _profile);

        Assert.AreEqual(0.65f, inactive.R2ThrottleResistance, 0.001f);
        Assert.IsFalse(inactive.AbsPulse);
        Assert.IsTrue(active.AbsPulse);
    }

    [TestMethod]
    public void ConfigurationStore_CreatesAndReloadsDefaults()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");

        try
        {
            var initial = UserConfigurationStore.LoadOrCreate(path);
            var reloaded = UserConfigurationStore.LoadOrCreate(path);

            Assert.AreEqual(1, reloaded.SchemaVersion);
            Assert.AreEqual(initial.HapticSettings, reloaded.HapticSettings);
            Assert.AreEqual(initial.VehicleProfile, reloaded.VehicleProfile);
            Assert.IsTrue(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
