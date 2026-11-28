using Provident.Foundations.VisualStudio.Analysis.Naming;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VS.Toolset.UI.Controls;

/// <summary>
/// Displays an interactive report for a naming analysis result.
/// </summary>
public partial class NamingReportView : UserControl
{
    /// <summary>
    /// Identifies the result displayed by the report.
    /// </summary>
    public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
        nameof(Result), typeof(NamingSolutionAnalysisResult), typeof(NamingReportView),
        new PropertyMetadata(null, OnResultChanged));

    /// <summary>Identifies the high-contrast state used by report controls.</summary>
    public static readonly DependencyProperty IsHighContrastProperty = DependencyProperty.Register(
        nameof(IsHighContrast), typeof(bool), typeof(NamingReportView), new PropertyMetadata(false));

    /// <summary>Gets whether Windows high contrast is enabled.</summary>
    public bool IsHighContrast => (bool)GetValue(IsHighContrastProperty);

    /// <summary>
    /// Initializes a new report view.
    /// </summary>
    public NamingReportView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
        ApplyHighContrast();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        SystemParameters.StaticPropertyChanged -= OnSystemParametersChanged;
    }

    private void OnSystemParametersChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SystemParameters.HighContrast))
            ApplyHighContrast();
    }

    private void ApplyHighContrast()
    {
        SetValue(IsHighContrastProperty, SystemParameters.HighContrast);
        string[] keys = ["WindowBrush", "SurfaceBrush", "RaisedBrush", "BorderBrush", "TextBrush", "MutedTextBrush", "AccentBrush", "SelectionBrush", "HoverBrush", "SeverityNeutralBrush", "SeverityErrorBrush", "SeverityWarningBrush", "SeverityInformationBrush", "ReportHeaderBackgroundBrush", "ReportHeaderForegroundBrush", "ReportHeaderMutedBrush"];
        if (!SystemParameters.HighContrast)
        {
            foreach (string key in keys)
                Resources.Remove(key);
            return;
        }

        Resources["WindowBrush"] = SystemColors.WindowBrush;
        Resources["SurfaceBrush"] = SystemColors.WindowBrush;
        Resources["RaisedBrush"] = SystemColors.WindowBrush;
        Resources["BorderBrush"] = SystemColors.WindowTextBrush;
        Resources["TextBrush"] = SystemColors.WindowTextBrush;
        Resources["MutedTextBrush"] = SystemColors.WindowTextBrush;
        Resources["AccentBrush"] = SystemColors.HighlightBrush;
        Resources["SelectionBrush"] = SystemColors.HighlightBrush;
        Resources["HoverBrush"] = SystemColors.WindowBrush;
        Resources["SeverityNeutralBrush"] = SystemColors.WindowTextBrush;
        Resources["SeverityErrorBrush"] = SystemColors.WindowTextBrush;
        Resources["SeverityWarningBrush"] = SystemColors.WindowTextBrush;
        Resources["SeverityInformationBrush"] = SystemColors.WindowTextBrush;
        Resources["ReportHeaderBackgroundBrush"] = SystemColors.WindowBrush;
        Resources["ReportHeaderForegroundBrush"] = SystemColors.WindowTextBrush;
        Resources["ReportHeaderMutedBrush"] = SystemColors.WindowTextBrush;
    }

    /// <summary>
    /// Gets the report's filter and selection state.
    /// </summary>
    public NamingReportViewModel Report { get; } = new();

    /// <summary>
    /// Gets or sets the analysis result displayed by the report.
    /// </summary>
    public NamingSolutionAnalysisResult? Result
    {
        get => (NamingSolutionAnalysisResult?)GetValue(ResultProperty);
        set => SetValue(ResultProperty, value);
    }

    private static void OnResultChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        ((NamingReportView)sender).Report.SetResult((NamingSolutionAnalysisResult?)e.NewValue);
    }

    private void ResetSearch(object sender, RoutedEventArgs e) => ClearSearch();

    private void ClearSearch()
    {
        SearchBox.Clear();
        SearchBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        Report.ResetFilters();
    }

    private void ShowRelatedObservation(object sender, RoutedEventArgs e)
    {
        NamingObservation? observation = Report.RelatedObservation;
        if (observation is null)
        {
            MessageBox.Show("The related name is not available in this result.", "Naming report", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        ClearSearch();
        Report.SelectedObservation = observation;
        ReportTabs.SelectedIndex = 3;
    }

    private void ShowRelatedPattern(object sender, RoutedEventArgs e)
    {
        NamingPattern? pattern = Report.RelatedPattern;
        if (pattern is null)
        {
            MessageBox.Show("The related pattern is not available in this result.", "Naming report", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        ClearSearch();
        Report.SelectedPattern = pattern;
        ReportTabs.SelectedIndex = 2;
    }

    private void OpenViolationSource(object sender, RoutedEventArgs e) => OpenSource(Report.SelectedViolation);
    private void OpenObservationSource(object sender, RoutedEventArgs e) => OpenSource(Report.SelectedObservation);
    private void OpenDiagnosticSource(object sender, RoutedEventArgs e) => OpenSource(Report.SelectedDiagnostic);
    private void OpenHierarchySource(object? sender, NamingHierarchySourceNavigationEventArgs e) => OpenSource(e.Item);

    private void OpenSelectedSource(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid && e.OriginalSource is DependencyObject source &&
            ItemsControl.ContainerFromElement(grid, source) is DataGridRow row)
            OpenSource(row.Item);
    }

    private void OpenSource(object? item)
    {
        NamingSourceSpan? location = item switch
        {
            NamingViolation violation => violation.Location,
            NamingObservation observation => observation.Location,
            NamingDiagnostic diagnostic => diagnostic.Location,
            _ => null
        };
        string? project = item switch
        {
            NamingViolation violation => violation.Project?.Name,
            NamingObservation observation => observation.SourceFile?.Project?.Name,
            _ => null
        };
        if (location is null || string.IsNullOrWhiteSpace(location.FilePath) || location.StartLine < 1)
        {
            MessageBox.Show("A source path and line are not available for the selected item.", "Naming report", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (Window.GetWindow(this) is MainWindow window)
            window.OpenSource(location.FilePath, location.StartLine, project);
        else
            MessageBox.Show("Source navigation is unavailable in this host.", "Naming report", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
