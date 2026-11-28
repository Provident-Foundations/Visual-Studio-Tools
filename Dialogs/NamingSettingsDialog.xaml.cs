using Provident.Foundations.VisualStudio.Analysis.Naming;
using System.Globalization;
using System.Windows;

namespace VS.Toolset.UI.Dialogs;

/// <summary>Edits the settings applied to the next naming analysis.</summary>
public partial class NamingSettingsDialog : Window
{
    /// <summary>Creates a settings dialog from the current configuration.</summary>
    public NamingSettingsDialog(NamingAnalysisConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        InitializeComponent();
        Configuration = configuration;
        IncludeXaml.IsChecked = configuration.IncludeXaml;
        IncludeGenerated.IsChecked = configuration.IncludeGeneratedFiles;
        MinimumObservations.Text = configuration.MinimumObservationsPerPattern.ToString(CultureInfo.CurrentCulture);
        MinimumDominance.Text = configuration.MinimumDominancePercentage.ToString(CultureInfo.CurrentCulture);
        MinimumConfidence.Text = configuration.MinimumViolationConfidence.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>Gets the applied configuration.</summary>
    public NamingAnalysisConfiguration Configuration { get; private set; }

    private void ApplySettings(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(MinimumObservations.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out int observations) || observations < 1 ||
            !double.TryParse(MinimumDominance.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double dominance) || !double.IsFinite(dominance) || dominance < 0 || dominance > 1 ||
            !double.TryParse(MinimumConfidence.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double confidence) || !double.IsFinite(confidence) || confidence < 0 || confidence > 1)
        {
            MessageBox.Show(this, "Enter at least one observation and values between 0 and 1 for dominance and confidence.", "Invalid settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Configuration = new NamingAnalysisConfiguration
        {
            MinimumObservationsPerPattern = observations,
            MinimumDominancePercentage = dominance,
            MinimumViolationConfidence = confidence,
            IncludeXaml = IncludeXaml.IsChecked == true,
            IncludeGeneratedFiles = IncludeGenerated.IsChecked == true,
            IncludeReferences = Configuration.IncludeReferences,
            PatternWeighting = Configuration.PatternWeighting,
            ExcludedFilePatterns = Configuration.ExcludedFilePatterns,
            ExcludedNamespacePrefixes = Configuration.ExcludedNamespacePrefixes,
            KnownPrefixes = Configuration.KnownPrefixes,
            KnownSuffixes = Configuration.KnownSuffixes
        };
        DialogResult = true;
    }
}
