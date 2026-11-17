using System.Text.Json;
using System.Text.Json.Serialization;

namespace EuroTruckHaptics.Configuration;

public static class UserConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ETS2DualSense",
        "settings.json");

    public static UserConfiguration LoadOrCreate(string? path = null)
    {
        path ??= DefaultPath;
        if (File.Exists(path))
        {
            var configuration = JsonSerializer.Deserialize<UserConfiguration>(
                File.ReadAllText(path),
                JsonOptions) ?? throw new InvalidDataException("The user configuration file is empty.");
            if (configuration.SchemaVersion != 1)
            {
                throw new InvalidDataException(
                    $"Unsupported settings schema version {configuration.SchemaVersion}.");
            }

            if (configuration.HapticSettings is null || configuration.VehicleProfile is null)
            {
                throw new InvalidDataException("The user configuration is missing required settings or profile data.");
            }

            return configuration;
        }

        var defaults = new UserConfiguration();
        Save(path, defaults);
        return defaults;
    }

    private static void Save(string path, UserConfiguration configuration)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("The configuration file must have a parent directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = fullPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(configuration, JsonOptions));
        File.Move(temporaryPath, fullPath, overwrite: true);
    }
}
