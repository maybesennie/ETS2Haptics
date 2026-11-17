using System.Globalization;
using System.Windows.Media;
using System.Windows.Threading;
using EuroTruckHaptics.Configuration;
using EuroTruckHaptics.Haptics;
using EuroTruckHaptics.Telemetry;

namespace EuroTruckHaptics;

public partial class MainWindow : System.Windows.Window
{
    private readonly ScsSharedMemorySource _telemetrySource = new();
    private readonly HapticEngine _hapticEngine = new();
    private readonly HapticSettings _hapticSettings;
    private readonly VehicleHapticProfile _vehicleProfile;
    private readonly DispatcherTimer _telemetryTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(100)
    };

    public MainWindow(UserConfiguration configuration)
    {
        _hapticSettings = configuration.HapticSettings;
        _vehicleProfile = configuration.VehicleProfile;
        InitializeComponent();
        _telemetryTimer.Tick += UpdateTelemetry;
        Closed += (_, _) =>
        {
            _telemetryTimer.Stop();
            _telemetrySource.Dispose();
        };
        _telemetryTimer.Start();
    }

    private void UpdateTelemetry(object? sender, EventArgs e)
    {
        var result = _telemetrySource.Read();
        ConnectionBannerText.Text = result.Message;
        ConnectionBannerText.Foreground = result.State == TelemetryConnectionState.Connected
            ? (Brush)FindResource("AccentBrush")
            : new SolidColorBrush(Color.FromRgb(246, 199, 122));
        TelemetryRateText.Text = result.UpdateFrequencyHz is { } rate
            ? $"Telemetry update rate {rate:F1} Hz (measured)"
            : "Telemetry update rate: --";

        if (result.Sample is not { } sample)
        {
            ClearTelemetry();
            return;
        }

        SpeedValue.Text = sample.SpeedMetersPerSecond is { } speed
            ? (speed * 3.6d).ToString("F0", CultureInfo.InvariantCulture)
            : "--";
        RpmValue.Text = sample.EngineRpm?.ToString("F0", CultureInfo.InvariantCulture) ?? "--";
        GearValue.Text = sample.Gear?.ToString(CultureInfo.InvariantCulture) ?? "--";
        TruckValue.Text = $"Truck: {JoinIdentity(sample.TruckBrand, sample.TruckName)}";
        SurfaceValue.Text = $"Road surface: {sample.Surface?.ToString() ?? "Unknown"}";
        EventStatus.Text = sample.AbsActive is null
            ? "Local targets calculated from available telemetry. ABS status is not exposed by this plugin."
            : "Local targets calculated from live telemetry.";

        var frame = _hapticEngine.Process(sample, _hapticSettings, _vehicleProfile);
        LeftActuatorBar.Value = frame.LeftActuator;
        RightActuatorBar.Value = frame.RightActuator;
        L2Bar.Value = frame.L2BrakeResistance;
        R2Bar.Value = frame.R2ThrottleResistance;
    }

    private void ClearTelemetry()
    {
        SpeedValue.Text = "--";
        RpmValue.Text = "--";
        GearValue.Text = "--";
        TruckValue.Text = "Truck: unavailable";
        SurfaceValue.Text = "Road surface: unavailable";
        EventStatus.Text = "Waiting for live telemetry";
        LeftActuatorBar.Value = 0;
        RightActuatorBar.Value = 0;
        L2Bar.Value = 0;
        R2Bar.Value = 0;
    }

    private static string JoinIdentity(string? brand, string? name) =>
        string.Join(" ", new[] { brand, name }.Where(value => !string.IsNullOrWhiteSpace(value)));
}
