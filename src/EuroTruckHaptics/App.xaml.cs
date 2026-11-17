using System.IO;
using System.Windows;
using EuroTruckHaptics.Configuration;

namespace EuroTruckHaptics;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var configuration = UserConfigurationStore.LoadOrCreate();
            new MainWindow(configuration).Show();
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            MessageBox.Show(
                $"Could not load ETS2 DualSense settings:\n{exception.Message}",
                "ETS2 DualSense Haptics",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }
}
