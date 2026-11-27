using Provident.Foundations.VisualStudio.Analysis.Naming;
using VS.Toolset.UI;

namespace VisualStudioToolset.Tests;

[TestClass]
[DoNotParallelize]
public sealed class NamingReportFilterTests
{
    [STATestMethod]
    public void DimensionsApplyOnlyToSupportedSectionsAndCombine()
    {
        NamingProjectIdentity alpha = new() { Name = "Alpha", FilePath = @"C:\src\alpha.csproj" };
        NamingProjectIdentity beta = new() { Name = "Beta", FilePath = @"C:\src\beta.csproj" };
        using NamingSolutionAnalysisResult result = new()
        {
            Projects = [new() { Project = alpha, Status = NamingAnalysisStatus.Completed }, new() { Project = beta, Status = NamingAnalysisStatus.Failed }],
            Observations = [
                new() { Id = "a", Name = "One", SourceFile = new() { Project = alpha }, Language = NamingSourceLanguage.CSharp, Kind = NamingSymbolKind.Class, Role = NamingNameRole.Declaration, Classification = new() { Style = NamingStyle.PascalCase } },
                new() { Id = "b", Name = "Two", SourceFile = new() { Project = beta }, Language = NamingSourceLanguage.Xaml, Kind = NamingSymbolKind.Property, Role = NamingNameRole.Reference, Classification = new() { Style = NamingStyle.CamelCase } }],
            Patterns = [new() { Scope = new() { ProjectName = "Alpha", Language = NamingSourceLanguage.CSharp, SymbolKind = NamingSymbolKind.Class }, DominantStyle = NamingStyle.PascalCase }, new() { Scope = new(), DominantStyle = NamingStyle.PascalCase }],
            Violations = [new() { Name = "One", Project = alpha, ActualStyle = NamingStyle.PascalCase, Severity = NamingViolationSeverity.Warning }, new() { Name = "Two", Project = beta, ActualStyle = NamingStyle.CamelCase, Severity = NamingViolationSeverity.Error }],
            Diagnostics = [new() { ProjectPath = alpha.FilePath, Severity = NamingDiagnosticSeverity.Warning }, new() { ProjectPath = beta.FilePath, Severity = NamingDiagnosticSeverity.Error }, new() { Severity = NamingDiagnosticSeverity.Warning }]
        };
        NamingReportViewModel report = new();
        report.SetResult(result);
        report.ProjectFilter = "alpha";
        report.LanguageFilter = NamingSourceLanguage.CSharp;
        report.KindFilter = NamingSymbolKind.Class;
        report.RoleFilter = NamingNameRole.Declaration;
        report.StyleFilter = NamingStyle.PascalCase;
        report.SeverityFilter = NamingViolationSeverity.Warning;
        report.StatusFilter = NamingAnalysisStatus.Completed;

        Assert.AreEqual(1, report.ObservationsState.VisibleCount);
        Assert.AreEqual(2, report.PatternsState.VisibleCount);
        Assert.AreEqual(1, report.ViolationsState.VisibleCount);
        Assert.AreEqual(1, report.ProjectsState.VisibleCount);
        Assert.AreEqual(2, report.DiagnosticsState.VisibleCount);
        Assert.AreEqual(2, report.ObservationsState.TotalCount);
        Assert.AreEqual("All", report.ProjectOptions[0].Label);
        Assert.IsNull(report.ProjectOptions[0].Value);
        CollectionAssert.Contains(report.ProjectOptions.Select(option => option.Value).ToArray(), "Alpha");
        Assert.IsNull(report.LanguageOptions[0].Value);
        CollectionAssert.Contains(report.LanguageOptions.Select(option => option.Value).ToArray(), NamingSourceLanguage.Xaml);

        report.SearchText = "two";
        Assert.IsTrue(report.ObservationsState.IsNoMatch);
        Assert.IsFalse(report.ObservationsState.IsEmpty);
        report.ResetFilters();
        Assert.AreEqual(string.Empty, report.SearchText);
        Assert.IsNull(report.ProjectFilter);
        Assert.IsNull(report.LanguageFilter);
        Assert.IsNull(report.KindFilter);
        Assert.IsNull(report.RoleFilter);
        Assert.IsNull(report.StyleFilter);
        Assert.IsNull(report.SeverityFilter);
        Assert.IsNull(report.StatusFilter);
        Assert.AreEqual(2, report.ObservationsState.VisibleCount);
        Assert.AreEqual(3, report.DiagnosticsState.VisibleCount);

        report.LanguageFilter = NamingSourceLanguage.Xaml;
        Assert.AreEqual(1, report.ObservationsState.VisibleCount);
        Assert.AreEqual(1, report.PatternsState.VisibleCount);
        Assert.AreEqual(2, report.ViolationsState.VisibleCount);
        report.ResetFilters();
        report.KindFilter = NamingSymbolKind.Property;
        Assert.AreEqual(1, report.ObservationsState.VisibleCount);
        Assert.AreEqual(1, report.PatternsState.VisibleCount);
        report.ResetFilters();
        report.RoleFilter = NamingNameRole.Reference;
        Assert.AreEqual(1, report.ObservationsState.VisibleCount);
        Assert.AreEqual(2, report.PatternsState.VisibleCount);
        report.ResetFilters();
        report.StyleFilter = NamingStyle.CamelCase;
        Assert.AreEqual(1, report.ObservationsState.VisibleCount);
        Assert.AreEqual(0, report.PatternsState.VisibleCount);
        Assert.AreEqual(1, report.ViolationsState.VisibleCount);
        Assert.AreEqual(3, report.DiagnosticsState.VisibleCount);
        report.ResetFilters();
        report.SeverityFilter = NamingViolationSeverity.Error;
        Assert.AreEqual(1, report.ViolationsState.VisibleCount);
        Assert.AreEqual(1, report.DiagnosticsState.VisibleCount);
        Assert.AreEqual(2, report.ObservationsState.VisibleCount);
        report.ResetFilters();
        report.StatusFilter = NamingAnalysisStatus.Failed;
        Assert.AreEqual(1, report.ProjectsState.VisibleCount);
        Assert.AreEqual(2, report.ObservationsState.VisibleCount);
        report.SetResult(null);
    }

    [STATestMethod]
    public void ComboBoxCanSelectAndClearFilter()
    {
        NamingReportViewModel report = new();
        System.Windows.Controls.ComboBox combo = new()
        {
            ItemsSource = report.LanguageOptions,
            DisplayMemberPath = "Label",
            SelectedValuePath = "Value"
        };
        combo.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedValueProperty,
            new System.Windows.Data.Binding(nameof(report.LanguageFilter)) { Source = report, Mode = System.Windows.Data.BindingMode.TwoWay });
        combo.SelectedItem = report.LanguageOptions.Single(option => option.Value == NamingSourceLanguage.Xaml);
        Assert.AreEqual(NamingSourceLanguage.Xaml, report.LanguageFilter);
        combo.SelectedItem = report.LanguageOptions[0];
        Assert.IsNull(report.LanguageFilter);
        Assert.AreEqual("All", ((NamingReportFilterOption<NamingSourceLanguage?>)combo.SelectedItem).Label);
    }

    [STATestMethod]
    public void UnknownDiagnosticProjectRemainsVisible()
    {
        NamingProjectIdentity alpha = new() { Name = "Alpha", FilePath = @"C:\src\alpha.csproj" };
        using NamingSolutionAnalysisResult result = new()
        {
            Observations = [new() { SourceFile = new() { Project = alpha } }],
            Diagnostics = [new() { ProjectPath = @"C:\src\alpha.csproj" }, new() { ProjectPath = @"C:\unknown.csproj" }]
        };
        NamingReportViewModel report = new();
        report.SetResult(result);
        report.ProjectFilter = "other";
        Assert.AreEqual(1, report.DiagnosticsState.VisibleCount);
        report.ProjectFilter = "Alpha";
        Assert.AreEqual(2, report.DiagnosticsState.VisibleCount);
        report.SetResult(null);
    }

    [STATestMethod]
    public void ResultReplacementPreservesFiltersAndUpdatesStateAndLinks()
    {
        NamingObservation first = new() { Id = "id", Name = "Alpha" };
        NamingObservation second = new() { Id = "id", Name = "Beta" };
        NamingPattern pattern = new() { Id = "pattern", ObservationIds = ["id"] };
        NamingViolation violation = new() { ObservationId = "id", PatternId = "pattern" };
        using NamingSolutionAnalysisResult original = new() { Observations = [first, second], Patterns = [pattern], Violations = [violation] };
        using NamingSolutionAnalysisResult replacement = new() { Observations = [new() { Name = "Gamma" }] };
        NamingReportViewModel report = new();
        List<string?> changes = [];
        report.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        Assert.IsFalse(report.HasResult);
        report.SetResult(original);
        report.SelectedViolation = violation;
        report.SelectedPattern = pattern;
        report.SearchText = "missing";
        Assert.AreSame(first, report.RelatedObservation);
        Assert.AreSame(pattern, report.RelatedPattern);
        CollectionAssert.AreEqual(new[] { first, second }, report.PatternObservations.ToArray());
        Assert.IsTrue(report.ObservationsState.IsNoMatch);
        Assert.IsTrue(report.DiagnosticsState.IsEmpty);
        Assert.IsFalse(report.DiagnosticsState.IsNoMatch);
        report.SetResult(replacement);
        Assert.IsTrue(report.HasResult);
        Assert.AreEqual("missing", report.SearchText);
        Assert.IsNull(report.SelectedPattern);
        Assert.IsNull(report.SelectedViolation);
        Assert.IsNull(report.RelatedObservation);
        Assert.AreEqual(0, report.PatternObservations.Count);
        Assert.AreEqual(1, report.ObservationsState.TotalCount);
        Assert.IsTrue(report.ObservationsState.IsNoMatch);
        CollectionAssert.Contains(changes, nameof(report.ObservationsState));
        CollectionAssert.Contains(changes, nameof(report.Counts));
        report.SetResult(null);
        Assert.IsFalse(report.HasResult);
        Assert.IsTrue(report.ObservationsState.IsEmpty);
    }
}
