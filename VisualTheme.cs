using System.Windows;
using System.Windows.Media;

namespace VS.Toolset.UI;

public enum VisualTheme
{
    Dark,
    Light,
    ProvidentDark,
    ProvidentLight
}

public static class VisualThemeManager
{
    private static readonly string[] BrushKeys =
    [
        "WindowBrush", "SurfaceBrush", "RaisedBrush", "BorderBrush", "TitleTextBrush", "TextBrush", "MutedTextBrush",
        "AccentBrush", "SelectionBrush", "HoverBrush", "EditorBackgroundBrush", "EditorForegroundBrush",
        "OverlayBrush", "SeverityNeutralBrush", "SeverityErrorBrush", "SeverityWarningBrush", "SeverityInformationBrush"
    ];

    private static ResourceDictionary? _activePalette;

    public static VisualTheme CurrentTheme { get; private set; } = VisualTheme.Dark;

    public static void ApplyTheme(Application application, VisualTheme theme)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (!Enum.IsDefined(theme))
            throw new ArgumentOutOfRangeException(nameof(theme), theme, "The visual theme is not supported.");
        if (_activePalette is not null && theme == CurrentTheme)
            return;

        Uri source = new($"/VSToolset;component/Themes/{theme}Theme.xaml", UriKind.Relative);
        ResourceDictionary palette = new() { Source = source };
        foreach (string key in BrushKeys)
        {
            if (palette[key] is not SolidColorBrush)
                throw new InvalidOperationException($"The '{key}' brush is missing from a visual theme.");
        }
        if (palette["HeaderGradient"] is not LinearGradientBrush)
            throw new InvalidOperationException("The 'HeaderGradient' brush is missing from a visual theme.");

        var dictionaries = application.Resources.MergedDictionaries;
        if (_activePalette is null)
        {
            dictionaries.Add(palette);
        }
        else
        {
            int index = dictionaries.IndexOf(_activePalette);
            if (index < 0)
                throw new InvalidOperationException("The active visual theme is no longer in the application resources.");

            dictionaries[index] = palette;
        }

        _activePalette = palette;
        CurrentTheme = theme;
    }
}
