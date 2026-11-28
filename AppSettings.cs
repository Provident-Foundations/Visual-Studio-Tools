using Provident.Foundations.Abstractions;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VS.Toolset.UI;

public sealed class AppSettings : DisposableObjectBase
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Provident Foundations",
        "Visual Studio Toolset",
        "settings.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public VisualTheme Theme { get; set; } = VisualTheme.Dark;

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return new AppSettings();

        AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), SerializerOptions);
        if (settings is null || !Enum.IsDefined(settings.Theme))
            throw new InvalidDataException("The saved visual theme is invalid.");
        return settings;
    }

    public void Save()
    {
        string? directory = Path.GetDirectoryName(SettingsPath);
        if (string.IsNullOrEmpty(directory))
            throw new InvalidOperationException("The application settings directory is unavailable.");

        Directory.CreateDirectory(directory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, SerializerOptions));
    }
}
