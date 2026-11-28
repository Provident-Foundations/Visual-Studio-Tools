using Microsoft.Build.Locator;
using System.IO;
using System.Windows;
using VS.Toolset.UI;

namespace XAML.ID.Verifier.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public AppSettings Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        // MSBuild must be registered before any Microsoft.Build types are
        // loaded so the Locator can resolve the installed toolset.
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        try
        {
            Settings = AppSettings.Load();
            VisualThemeManager.ApplyTheme(this, Settings.Theme);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidDataException)
        {
            Settings = new AppSettings();
            VisualThemeManager.ApplyTheme(this, Settings.Theme);
            MessageBox.Show($"The saved application settings could not be loaded. The Dark theme will be used.\n\n{exception.Message}", "Settings unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        base.OnStartup(e);
    }
}