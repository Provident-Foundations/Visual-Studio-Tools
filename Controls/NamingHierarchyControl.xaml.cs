using Provident.Foundations.VisualStudio.Analysis.Naming;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VS.Toolset.UI.Controls;

/// <summary>
/// Displays naming analysis results in a solution, project, and project-data hierarchy.
/// The control intentionally inherits its <see cref="FrameworkElement.DataContext"/>.
/// </summary>
public partial class NamingHierarchyControl : UserControl
{
    /// <summary>
    /// Initializes a new hierarchy control.
    /// </summary>
    public NamingHierarchyControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised when a user requests source navigation for a violation or diagnostic row.
    /// The event carries the existing naming model instance; the host owns navigation.
    /// </summary>
    public event EventHandler<NamingHierarchySourceNavigationEventArgs>? SourceNavigationRequested;

    private void OnProjectExpanded(object sender, RoutedEventArgs e)
    {
        if (sender is Expander { DataContext: NamingSolutionProjectResult project } && DataContext is NamingReportViewModel report)
            report.SelectedProject = project;
    }

    private void OnSourceRowMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid || e.OriginalSource is not DependencyObject source)
            return;

        DataGridRow? row = ItemsControl.ContainerFromElement(grid, source) as DataGridRow;
        if (row is not null && row.Item is NamingViolation or NamingDiagnostic)
        {
            RequestSourceNavigation(row.Item);
            e.Handled = true;
        }
    }

    private void OnSourceRowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not DataGrid grid || grid.SelectedItem is not (NamingViolation or NamingDiagnostic))
            return;

        RequestSourceNavigation(grid.SelectedItem);
        e.Handled = true;
    }

    private void RequestSourceNavigation(object item) =>
        SourceNavigationRequested?.Invoke(this, new NamingHierarchySourceNavigationEventArgs(item));
}

/// <summary>
/// Carries an existing naming report item to the host's source-navigation mechanism.
/// </summary>
public sealed class NamingHierarchySourceNavigationEventArgs : EventArgs
{
    internal NamingHierarchySourceNavigationEventArgs(object item) => Item = item;

    /// <summary>
    /// Gets the selected <see cref="NamingViolation"/> or <see cref="NamingDiagnostic"/>.
    /// </summary>
    public object Item { get; }
}
