using Provident.Foundations.Wpf.Controls;
using System.Windows;
using XAML.ID.Verifier.UI;

namespace VS.Toolset.UI;

/// <summary>
/// Applies and saves application theme selections.
/// </summary>
internal sealed class ThemeService
{
    /// <summary>
    /// Applies a theme to the application.
    /// </summary>
    /// <param name="theme">
    /// The theme to apply.
    /// </param>
    public void Apply(VisualTheme theme)
    {
        VisualThemeManager.ApplyTheme(Application.Current, theme);
    }

    /// <summary>
    /// Saves the theme when the current application provides settings.
    /// </summary>
    /// <param name="theme">
    /// The selected theme.
    /// </param>
    public void Save(VisualTheme theme)
    {
        if (Application.Current is App application)
        {
            application.Settings.Theme = theme;
            application.Settings.Save();
        }
    }
}
