using Provident.Foundations.VisualStudio.Analysis.Naming;
using VS.Toolset.UI;
using VS.Toolset.UI.Controls;

namespace VisualStudioToolset.Tests;

/// <summary>
/// Verifies naming report filtering, selection, and result replacement.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class NamingReportViewModelTests
{
    /// <summary>
    /// Verifies case-insensitive filtering and reset across report lists.
    /// </summary>
    [STATestMethod]
    public void SearchFiltersListsAndResetRestoresCounts()
    {
        using NamingSolutionAnalysisResult result = new()
        {
            Observations = [new NamingObservation { Name = "Alpha" }, new NamingObservation { Name = "Beta" }],
            Violations = [new NamingViolation { Name = "Alpha" }, new NamingViolation { Name = "Beta" }],
            Diagnostics = [new NamingDiagnostic { Message = "ALPHA diagnostic" }]
        };
        NamingReportViewModel report = new();
        report.SetResult(result);

        report.SearchText = " alpha ";
        Assert.AreEqual(1, report.Observations.Cast<object>().Count());
        Assert.AreEqual(1, report.Violations.Cast<object>().Count());
        Assert.AreEqual(1, report.Diagnostics.Cast<object>().Count());
        StringAssert.Contains(report.Counts, "Violations 1/2");
        StringAssert.Contains(report.Counts, "Names 1/2");

        report.SearchText = string.Empty;
        Assert.AreEqual(2, report.Observations.Cast<object>().Count());
        Assert.AreEqual(2, report.Violations.Cast<object>().Count());
        StringAssert.Contains(report.Counts, "Names 2/2");
        report.SetResult(null);
    }

    /// <summary>
    /// Verifies related items use IDs and disappear when the result changes.
    /// </summary>
    [STATestMethod]
    public void SelectionFindsRelatedItemsAndReplacementClearsThem()
    {
        NamingObservation observation = new() { Id = "observation", Name = "Example" };
        NamingPattern pattern = new() { Id = "pattern" };
        NamingViolation violation = new() { ObservationId = "observation", PatternId = "pattern" };
        using NamingSolutionAnalysisResult first = new() { Observations = [observation], Patterns = [pattern], Violations = [violation] };
        using NamingSolutionAnalysisResult second = new();
        NamingReportViewModel report = new();
        report.SetResult(first);
        report.SelectedViolation = violation;

        Assert.AreSame(observation, report.RelatedObservation);
        Assert.AreSame(pattern, report.RelatedPattern);
        report.SetResult(second);
        Assert.IsNull(report.SelectedViolation);
        Assert.IsNull(report.RelatedObservation);
        Assert.IsNull(report.RelatedPattern);
        Assert.AreEqual(0, report.Violations.Cast<object>().Count());
        report.SetResult(null);
        Assert.IsNull(report.Result);
    }

    /// <summary>
    /// Verifies missing identifiers do not link unrelated default-valued objects.
    /// </summary>
    [STATestMethod]
    public void MissingReferenceIdsDoNotMatchEmptyIds()
    {
        using NamingSolutionAnalysisResult result = new()
        {
            Observations = [new NamingObservation()],
            Patterns = [new NamingPattern()],
            Violations = [new NamingViolation()]
        };
        NamingReportViewModel report = new();
        report.SetResult(result);
        report.SelectedViolation = result.Violations[0];

        Assert.IsNull(report.RelatedObservation);
        Assert.IsNull(report.RelatedPattern);
        report.SetResult(null);
    }

    /// <summary>
    /// Verifies resetting a search also clears text awaiting the deferred update.
    /// </summary>
    [STATestMethod]
    public void ResetClearsPendingSearch()
    {
        using NamingSolutionAnalysisResult result = new() { Observations = [new NamingObservation { Name = "Example" }] };
        NamingReportView control = new() { Result = result };
        System.Windows.Controls.TextBox search = (System.Windows.Controls.TextBox)control.FindName("SearchBox");
        System.Windows.Controls.Button reset = (System.Windows.Controls.Button)control.FindName("ResetSearchButton");
        search.Text = "no match";
        reset.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

        System.Windows.Threading.DispatcherFrame frame = new();
        System.Windows.Threading.DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start();
        System.Windows.Threading.Dispatcher.PushFrame(frame);

        Assert.AreEqual(string.Empty, search.Text);
        Assert.AreEqual(string.Empty, control.Report.SearchText);
        Assert.AreEqual(1, control.Report.Observations.Cast<object>().Count());
        control.Result = null;
    }

    /// <summary>
    /// Verifies overview tokens are ranked, bounded and cleared on replacement.
    /// </summary>
    [STATestMethod]
    public void OverviewShowsTwentyMostFrequentTokens()
    {
        Dictionary<string, int> tokens = Enumerable.Range(1, 25).ToDictionary(value => $"token{value}", value => value);
        using NamingSolutionAnalysisResult result = new() { Statistics = new NamingStatistics { CommonTokens = tokens } };
        NamingReportViewModel report = new();
        report.SetResult(result);
        Assert.AreEqual(20, report.TopTokens.Count);
        Assert.AreEqual("token25", report.TopTokens[0].Key);
        Assert.AreEqual(25, report.TopTokens[0].Value);
        Assert.AreEqual("token6", report.TopTokens[19].Key);
        report.SetResult(null);
        Assert.AreEqual(0, report.TopTokens.Count);
    }

    /// <summary>
    /// Verifies that the report control loads and follows its result property.
    /// </summary>
    [STATestMethod]
    public void ControlLoadsAndTracksResult()
    {
        using NamingSolutionAnalysisResult result = new() { Observations = [new NamingObservation { Name = "Example" }] };
        NamingReportView control = new();
        control.Result = result;
        Assert.AreSame(result, control.Report.Result);
        Assert.AreEqual(1, control.Report.Observations.Cast<object>().Count());
        control.Result = null;
        Assert.IsNull(control.Report.Result);
        Assert.AreEqual(0, control.Report.Observations.Cast<object>().Count());
    }
}
