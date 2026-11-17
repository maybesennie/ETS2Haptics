using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using EuroTruckHaptics.Haptics;

namespace EuroTruckHaptics.Telemetry;

public sealed class ScsSharedMemorySource : IDisposable
{
    public const string MapName = @"Local\SCSTelemetry";
    public const uint SupportedPluginRevision = 12;

    private const int MapSize = 32 * 1024;
    private const int OffSdkActive = 0;
    private const int OffPaused = 4;
    private const int OffUpdateCounter = 8;
    private const int OffPluginRevision = 40;
    private const int OffRetarderStepCount = 76;
    private const int OffRetarderBrake = 108;
    private const int OffWheelSubstances = 120;
    private const int OffGear = 504;
    private const int OffSpeed = 948;
    private const int OffEngineRpm = 952;
    private const int OffThrottle = 960;
    private const int OffBrake = 964;
    private const int OffTruckBrand = 2364;
    private const int OffTruckName = 2492;
    private const int OffSubstances = 4400;
    private const int SubstanceCount = 25;
    private const int SubstanceNameLength = 64;

    private readonly string[] _substanceNames = new string[SubstanceCount];
    private readonly RoadSurface[] _surfaces = new RoadSurface[SubstanceCount];
    private readonly byte[] _brandBuffer = new byte[SubstanceNameLength];
    private readonly byte[] _brandBytes = new byte[SubstanceNameLength];
    private readonly byte[] _truckNameBuffer = new byte[SubstanceNameLength];
    private readonly byte[] _truckNameBytes = new byte[SubstanceNameLength];
    private MemoryMappedFile? _map;
    private MemoryMappedViewAccessor? _view;
    private long _nextOpenAttemptTicks;
    private long _lastCounterChangeTicks;
    private ulong _lastUpdateCounter;
    private double? _updateFrequencyHz;
    private bool _hasUpdateCounter;
    private bool _accessDenied;
    private string? _openFailure;
    private string? _truckBrand;
    private string? _truckName;

    public static IReadOnlyList<TelemetryCapability> Capabilities { get; } =
    [
        new("Speed", typeof(double), true, "truck_f.speed", ValidityRule: "Finite float.",
            FallbackBehavior: "Null when non-finite."),
        new("Engine RPM", typeof(double), true, "truck_f.engineRpm", ValidityRule: "Finite float.",
            FallbackBehavior: "Null when non-finite."),
        new("Throttle", typeof(double), true, "truck_f.userThrottle", ValidityRule: "Finite float, clamped to 0..1.",
            FallbackBehavior: "Null when non-finite."),
        new("Brake", typeof(double), true, "truck_f.userBrake", ValidityRule: "Finite float, clamped to 0..1.",
            FallbackBehavior: "Null when non-finite."),
        new("Gear", typeof(int), true, "truck_i.gear", ValidityRule: "Read as signed 32-bit integer.",
            FallbackBehavior: "No inferred gear."),
        new("Retarder", typeof(double), true, "truck_ui.retarderBrake / config_ui.retarderStepCount",
            ValidityRule: "Only available when configured step count is non-zero.",
            FallbackBehavior: "Null if no retarder steps are configured."),
        new("Road surface", typeof(RoadSurface), true, "truck_ui.truck_wheelSubstance + substances",
            ValidityRule: "Only recognized substance names are classified.",
            FallbackBehavior: "Unknown for unrecognized/missing substance."),
        new("Truck identity", typeof(string), true, "config_s.truckBrand + config_s.truckName",
            ValidityRule: "Null-terminated UTF-8 strings.",
            FallbackBehavior: "Null when empty."),
        new("Engine load", typeof(double), false, "Not present in plugin revision 12 shared-memory map",
            "Unavailable; no estimate is substituted.", FallbackBehavior: "Null."),
        new("Wheel slip", typeof(double), false, "Not present in plugin revision 12 shared-memory map",
            "Unavailable; no estimate is substituted.", FallbackBehavior: "Null."),
        new("ABS", typeof(bool), false, "Not present in plugin revision 12 shared-memory map",
            "Unavailable; no estimate is substituted.", FallbackBehavior: "Null.")
    ];

    public TelemetryReadResult Read()
    {
        var view = EnsureOpen();
        if (view is null)
        {
            if (_accessDenied)
            {
                return new(TelemetryConnectionState.AccessDenied, null,
                    "Access to the SCS telemetry map was denied.");
            }

            return new(TelemetryConnectionState.Disconnected, null,
                _openFailure ??
                "Waiting for Local\\SCSTelemetry. Install/enable the RenCloud SCS SDK plugin and run ETS2.");
        }

        try
        {
            var revision = view.ReadUInt32(OffPluginRevision);
            if (revision != SupportedPluginRevision)
            {
                return new(TelemetryConnectionState.UnsupportedPlugin, null,
                    $"Unsupported SCS telemetry plugin revision {revision}; expected {SupportedPluginRevision}.");
            }

            if (view.ReadByte(OffSdkActive) == 0 || view.ReadByte(OffPaused) != 0)
            {
                return new(TelemetryConnectionState.GameInactive, null,
                    "The telemetry plugin is present, but ETS2 telemetry is inactive or paused.");
            }

            var before = view.ReadUInt64(OffUpdateCounter);
            var speed = ReadFinite(view, OffSpeed);
            var rpm = ReadFinite(view, OffEngineRpm);
            var throttle = UnitValue(ReadFinite(view, OffThrottle));
            var brake = UnitValue(ReadFinite(view, OffBrake));
            var gear = view.ReadInt32(OffGear);
            var retarder = ReadRetarder(view);
            var surface = ReadSurface(view);
            ReadCachedString(view, OffTruckBrand, _brandBuffer, _brandBytes, ref _truckBrand);
            ReadCachedString(view, OffTruckName, _truckNameBuffer, _truckNameBytes, ref _truckName);
            var after = view.ReadUInt64(OffUpdateCounter);

            if (before != after)
            {
                return new(TelemetryConnectionState.InconsistentSnapshot, null,
                    "Telemetry changed during sampling; waiting for the next consistent update.");
            }

            var nowTicks = Stopwatch.GetTimestamp();
            if (!_hasUpdateCounter)
            {
                _lastUpdateCounter = after;
                _lastCounterChangeTicks = nowTicks;
                _hasUpdateCounter = true;
                _updateFrequencyHz = null;
            }
            else if (after > _lastUpdateCounter)
            {
                var elapsed = Stopwatch.GetElapsedTime(_lastCounterChangeTicks, nowTicks).TotalSeconds;
                if (elapsed > 0d)
                {
                    var rate = (after - _lastUpdateCounter) / elapsed;
                    _updateFrequencyHz = double.IsFinite(rate) && rate <= 1000d ? rate : null;
                }

                _lastUpdateCounter = after;
                _lastCounterChangeTicks = nowTicks;
            }
            else if (after < _lastUpdateCounter)
            {
                _lastUpdateCounter = after;
                _lastCounterChangeTicks = nowTicks;
                _updateFrequencyHz = null;
            }
            else if (Stopwatch.GetElapsedTime(_lastCounterChangeTicks, nowTicks) > TimeSpan.FromSeconds(2))
            {
                return new(TelemetryConnectionState.Stale, null,
                    "The SCS telemetry update counter has not advanced for two seconds.");
            }

            return new(TelemetryConnectionState.Connected,
                new TelemetrySample(
                    DateTimeOffset.UtcNow,
                    speed,
                    rpm,
                    EngineLoad: null,
                    Throttle: throttle,
                    Brake: brake,
                    Retarder: retarder,
                    AbsActive: null,
                    Surface: surface,
                    Gear: gear,
                    TruckBrand: _truckBrand,
                    TruckName: _truckName),
                "Connected to the RenCloud SCS shared-memory telemetry plugin.",
                _updateFrequencyHz);
        }
        catch (UnauthorizedAccessException)
        {
            CloseMap();
            return new(TelemetryConnectionState.AccessDenied, null,
                "Access to the SCS telemetry map was denied.");
        }
        catch (IOException exception)
        {
            CloseMap();
            return new(TelemetryConnectionState.Disconnected, null,
                $"The SCS telemetry map became unavailable: {exception.Message}");
        }
    }

    public void Dispose() => CloseMap();

    private MemoryMappedViewAccessor? EnsureOpen()
    {
        if (_view is not null)
        {
            return _view;
        }

        var now = Stopwatch.GetTimestamp();
        if (now < _nextOpenAttemptTicks)
        {
            return null;
        }

        _nextOpenAttemptTicks = now + Stopwatch.Frequency;
        _accessDenied = false;
        _openFailure = null;
        try
        {
            _map = MemoryMappedFile.OpenExisting(MapName, MemoryMappedFileRights.Read);
            _view = _map.CreateViewAccessor(0, MapSize, MemoryMappedFileAccess.Read);
            ReadSubstanceNames(_view);
            _hasUpdateCounter = false;
            return _view;
        }
        catch (FileNotFoundException)
        {
            CloseMap();
            _openFailure =
                "Waiting for Local\\SCSTelemetry. Install/enable the RenCloud SCS SDK plugin and run ETS2.";
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            CloseMap();
            _accessDenied = true;
            _openFailure = "Access to the SCS telemetry map was denied.";
            return null;
        }
        catch (IOException exception)
        {
            CloseMap();
            _openFailure = $"The SCS telemetry map could not be opened: {exception.Message}";
            return null;
        }
    }

    private double? ReadRetarder(MemoryMappedViewAccessor view)
    {
        var count = view.ReadUInt32(OffRetarderStepCount);
        if (count == 0)
        {
            return null;
        }

        return Math.Clamp(view.ReadUInt32(OffRetarderBrake) / (double)count, 0d, 1d);
    }

    private RoadSurface ReadSurface(MemoryMappedViewAccessor view)
    {
        var substanceIndex = view.ReadUInt32(OffWheelSubstances);
        if (substanceIndex >= _substanceNames.Length)
        {
            return RoadSurface.Unknown;
        }

        return _surfaces[substanceIndex];
    }

    private void ReadSubstanceNames(MemoryMappedViewAccessor view)
    {
        for (var index = 0; index < _substanceNames.Length; index++)
        {
            _substanceNames[index] = ReadString(
                view,
                OffSubstances + index * SubstanceNameLength,
                SubstanceNameLength);
            _surfaces[index] = ClassifySurface(_substanceNames[index]);
        }
    }

    private static RoadSurface ClassifySurface(string value)
    {
        if (value.Contains("cobblestone", StringComparison.OrdinalIgnoreCase)) return RoadSurface.Cobblestone;
        if (value.Contains("gravel", StringComparison.OrdinalIgnoreCase)) return RoadSurface.Gravel;
        if (value.Contains("concrete", StringComparison.OrdinalIgnoreCase)) return RoadSurface.Concrete;
        if (value.Contains("asphalt", StringComparison.OrdinalIgnoreCase)) return RoadSurface.Asphalt;
        if (value.Contains("mud", StringComparison.OrdinalIgnoreCase)) return RoadSurface.Mud;
        if (value.Contains("dirt", StringComparison.OrdinalIgnoreCase)) return RoadSurface.Dirt;
        if (value.Contains("snow", StringComparison.OrdinalIgnoreCase)) return RoadSurface.Snow;
        if (value.Contains("ice", StringComparison.OrdinalIgnoreCase)) return RoadSurface.Ice;
        return RoadSurface.Unknown;
    }

    private static double? ReadFinite(MemoryMappedViewAccessor view, int offset)
    {
        var value = view.ReadSingle(offset);
        return float.IsFinite(value) ? value : null;
    }

    private static double? UnitValue(double? value) =>
        value is { } number ? Math.Clamp(number, 0d, 1d) : null;

    private static string ReadString(MemoryMappedViewAccessor view, int offset, int length)
    {
        var bytes = new byte[length];
        view.ReadArray(offset, bytes, 0, bytes.Length);
        var terminator = Array.IndexOf(bytes, (byte)0);
        return Encoding.UTF8.GetString(bytes, 0, terminator < 0 ? bytes.Length : terminator).Trim();
    }

    private static void ReadCachedString(
        MemoryMappedViewAccessor view,
        int offset,
        byte[] buffer,
        byte[] previousBytes,
        ref string? cachedValue)
    {
        view.ReadArray(offset, buffer, 0, buffer.Length);
        if (buffer.AsSpan().SequenceEqual(previousBytes))
        {
            return;
        }

        buffer.CopyTo(previousBytes, 0);
        var terminator = Array.IndexOf(buffer, (byte)0);
        var length = terminator < 0 ? buffer.Length : terminator;
        var value = Encoding.UTF8.GetString(buffer, 0, length).Trim();
        cachedValue = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private void CloseMap()
    {
        _view?.Dispose();
        _map?.Dispose();
        _view = null;
        _map = null;
        _hasUpdateCounter = false;
    }
}
