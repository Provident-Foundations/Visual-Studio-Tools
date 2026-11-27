using Provident.Foundations.VisualStudio.Analysis.Naming;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using VS.Toolset.UI;
using VS.Toolset.UI.Controls;

namespace VisualStudioToolset.Tests;

[TestClass]
[DoNotParallelize]
public sealed class NamingReportInteractionTests
{
    [STATestMethod]
    public void FilterControlsAndEmptyStateFollowTheReport()
    {
        using NamingSolutionAnalysisResult result = new()
        {
            Observations = [new NamingObservation { Id = "a", Name = "Name", Language = NamingSourceLanguage.CSharp }],
            Violations = [new NamingViolation { ObservationId = "a", Name = "Name" }]
        };
        NamingReportView view = new() { Result = result };
        view.Measure(new Size(900, 600));
        view.Arrange(new Rect(0, 0, 900, 600));
        view.UpdateLayout();
        TabControl tabs = (TabControl)view.FindName("ReportTabs");
        tabs.SelectedIndex = 1;
        view.UpdateLayout();
        ComboBox language = (ComboBox)view.FindName("LanguageFilterBox");
        TextBlock empty = (TextBlock)view.FindName("ViolationsEmptyState");
        language.SelectedValue = NamingSourceLanguage.Xaml;
        view.UpdateLayout();

        Assert.AreEqual(NamingSourceLanguage.Xaml, view.Report.LanguageFilter);
        Assert.AreEqual(0, view.Report.ViolationsState.VisibleCount);
        Assert.AreEqual(Visibility.Visible, empty.Visibility);
        Assert.AreEqual("No results match the current filters.", empty.Text);

        Button reset = (Button)view.FindName("ResetSearchButton");
        reset.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        view.UpdateLayout();
        Assert.IsNull(view.Report.LanguageFilter);
        Assert.AreEqual(1, view.Report.ViolationsState.VisibleCount);
        Assert.AreEqual(Visibility.Collapsed, empty.Visibility);
        Assert.IsNotNull(view.FindName("ProjectHierarchy"));
        using NamingSolutionAnalysisResult emptyResult = new();
        view.Result = emptyResult;
        view.UpdateLayout();
        Assert.AreEqual("No entries were reported in this section.", empty.Text);
        view.Result = null;
        view.UpdateLayout();
        Assert.AreEqual("Run an analysis to view results.", empty.Text);
        Assert.AreEqual(Visibility.Visible, empty.Visibility);
    }

    [STATestMethod]
    public void HierarchySelectionTracksProjectFilters()
    {
        NamingSolutionProjectResult alpha = new() { Project = new NamingProjectIdentity { Name = "Alpha" } };
        NamingSolutionProjectResult beta = new() { Project = new NamingProjectIdentity { Name = "Beta" } };
        using NamingSolutionAnalysisResult result = new() { Projects = [alpha, beta] };
        NamingReportViewModel report = new();
        report.SetResult(result);
        NamingHierarchyControl hierarchy = new() { DataContext = report };
        hierarchy.Measure(new Size(900, 600));
        hierarchy.Arrange(new Rect(0, 0, 900, 600));
        hierarchy.UpdateLayout();
        ListBox projects = (ListBox)hierarchy.FindName("ProjectList");
        projects.SelectedItem = beta;
        Assert.AreSame(beta, report.SelectedProject);
        report.ProjectFilter = "Alpha";
        hierarchy.UpdateLayout();
        Assert.AreEqual(1, projects.Items.Count);
        Assert.AreSame(alpha, projects.Items[0]);
        Assert.AreSame(projects.SelectedItem, report.SelectedProject);
        Assert.AreNotSame(beta, report.SelectedProject);
        report.SetResult(null);
    }
}
