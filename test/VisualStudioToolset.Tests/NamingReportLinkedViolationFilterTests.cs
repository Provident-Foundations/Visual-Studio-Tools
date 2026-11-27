using Provident.Foundations.VisualStudio.Analysis.Naming;
using VS.Toolset.UI;

namespace VisualStudioToolset.Tests;

[TestClass]
public sealed class NamingReportLinkedViolationFilterTests
{
    [STATestMethod]
    public void LinkedObservationsSupplyDimensionsAndUnresolvedLinksRemainVisible()
    {
        NamingObservation xaml = new() { Id = "x", Language = NamingSourceLanguage.Xaml, Kind = NamingSymbolKind.XamlIdentifier, Role = NamingNameRole.XamlIdentifier };
        NamingObservation csharp = new() { Id = "c", Language = NamingSourceLanguage.CSharp, Kind = NamingSymbolKind.Class, Role = NamingNameRole.Declaration };
        NamingObservation emptyId = new() { Id = "", Language = NamingSourceLanguage.CSharp };
        NamingViolation linkedXaml = new() { ObservationId = "x", Name = "xaml" };
        NamingViolation linkedCsharp = new() { ObservationId = "c", Name = "csharp" };
        NamingViolation missing = new() { ObservationId = "missing", Name = "missing" };
        NamingViolation empty = new() { ObservationId = "", Name = "empty" };
        NamingViolation caseMismatch = new() { ObservationId = "X", Name = "case" };
        using NamingSolutionAnalysisResult result = new() { Observations = [xaml, csharp, emptyId], Violations = [linkedXaml, linkedCsharp, missing, empty, caseMismatch] };
        NamingReportViewModel report = new();
        report.SetResult(result);

        report.LanguageFilter = NamingSourceLanguage.Xaml;
        CollectionAssert.AreEqual(new[] { linkedXaml, missing, empty, caseMismatch }, report.Violations.Cast<NamingViolation>().ToArray());
        report.ResetFilters();
        report.KindFilter = NamingSymbolKind.Class;
        CollectionAssert.AreEqual(new[] { linkedCsharp, missing, empty, caseMismatch }, report.Violations.Cast<NamingViolation>().ToArray());
        report.ResetFilters();
        report.RoleFilter = NamingNameRole.XamlIdentifier;
        CollectionAssert.AreEqual(new[] { linkedXaml, missing, empty, caseMismatch }, report.Violations.Cast<NamingViolation>().ToArray());
        report.LanguageFilter = NamingSourceLanguage.CSharp;
        CollectionAssert.AreEqual(new[] { missing, empty, caseMismatch }, report.Violations.Cast<NamingViolation>().ToArray());
        report.SearchText = "missing";
        CollectionAssert.AreEqual(new[] { missing }, report.Violations.Cast<NamingViolation>().ToArray());
        report.SetResult(null);
    }

    [STATestMethod]
    public void ProjectUsesViolationFirstThenRelatedObservationAndExcludesUnknownProjects()
    {
        NamingProjectIdentity alpha = new() { Name = "Alpha" };
        NamingProjectIdentity beta = new() { Name = "Beta" };
        NamingObservation withProject = new() { Id = "x", SourceFile = new() { Project = alpha }, Language = NamingSourceLanguage.Xaml };
        NamingObservation noSource = new() { Id = "none" };
        NamingObservation noProject = new() { Id = "no-project", SourceFile = new() };
        NamingObservation blankName = new() { Id = "blank", SourceFile = new() { Project = new() { Name = " " } } };
        NamingViolation fallback = new() { ObservationId = "x", Name = "fallback" };
        NamingViolation blankDirectName = new() { ObservationId = "x", Project = new() { Name = " " } };
        NamingViolation direct = new() { ObservationId = "x", Project = beta };
        NamingViolation unresolved = new() { ObservationId = "missing", Project = alpha };
        NamingViolation unknown = new() { ObservationId = "none" };
        using NamingSolutionAnalysisResult result = new() { Observations = [withProject, noSource, noProject, blankName], Violations = [fallback, blankDirectName, direct, unresolved, unknown] };
        NamingReportViewModel report = new();
        report.SetResult(result);
        report.ProjectFilter = "ALPHA";
        CollectionAssert.AreEqual(new[] { withProject }, report.Observations.Cast<NamingObservation>().ToArray());
        CollectionAssert.AreEqual(new[] { fallback, blankDirectName, unresolved }, report.Violations.Cast<NamingViolation>().ToArray());
        Assert.AreEqual(3, report.ViolationsState.VisibleCount);
        Assert.AreEqual(5, report.ViolationsState.TotalCount);
        report.ProjectFilter = "beta";
        CollectionAssert.AreEqual(new[] { direct }, report.Violations.Cast<NamingViolation>().ToArray());
        Assert.IsTrue(report.ObservationsState.IsNoMatch);
        report.ResetFilters();
        Assert.AreEqual(4, report.ObservationsState.VisibleCount);
        Assert.AreEqual(5, report.ViolationsState.VisibleCount);
        report.SearchText = "Alpha";
        CollectionAssert.AreEqual(new[] { fallback, blankDirectName, direct, unresolved }, report.Violations.Cast<NamingViolation>().ToArray());
        report.SearchText = "Xaml";
        CollectionAssert.AreEqual(new[] { fallback, blankDirectName, direct }, report.Violations.Cast<NamingViolation>().ToArray());
        report.SetResult(null);
    }

    [STATestMethod]
    public void ReplacingResultRebuildsLinkedObservationIndexAndPreservesFilters()
    {
        NamingViolation violation = new() { ObservationId = "same" };
        using NamingSolutionAnalysisResult first = new() { Observations = [new() { Id = "same", Language = NamingSourceLanguage.Xaml }], Violations = [violation] };
        using NamingSolutionAnalysisResult second = new() { Observations = [new() { Id = "same", Language = NamingSourceLanguage.CSharp }], Violations = [new() { ObservationId = "same" }] };
        using NamingSolutionAnalysisResult third = new() { Violations = [new() { ObservationId = "same" }] };
        NamingReportViewModel report = new();
        report.SetResult(first);
        report.LanguageFilter = NamingSourceLanguage.Xaml;
        Assert.AreEqual(1, report.ViolationsState.VisibleCount);
        report.SetResult(second);
        Assert.AreEqual(0, report.ViolationsState.VisibleCount);
        Assert.IsTrue(report.ViolationsState.IsNoMatch);
        report.SetResult(third);
        Assert.AreEqual(1, report.ViolationsState.VisibleCount);
        Assert.AreEqual(NamingSourceLanguage.Xaml, report.LanguageFilter);
        report.SetResult(null);
    }
}
