using Provident.Foundations.VisualStudio.Analysis.CodeReview;
using Provident.Foundations.Wpf.Controls.ErrorList;
using VS.Toolset.UI;

namespace VisualStudioToolset.Tests;

/// <summary>
/// Verifies conversion of review findings for the error list.
/// </summary>
[TestClass]
public sealed class CodeReviewServiceTests
{
    /// <summary>
    /// Verifies severity, source location and finding identity for review entries.
    /// </summary>
    [TestMethod]
    public void GetEntriesPreservesFindingDetailsAndManualSeverity()
    {
        ReviewFinding error = new(default, "Rule", "Issue", @"C:\project", "source.cs", 12, false, ReviewSeverity.Error);
        ReviewFinding manual = new(default, "Manual", "Inspect", null, null, null, true, ReviewSeverity.Error);
        ReviewReport report = new(null, null, "Solution", [], [error, manual], DateTimeOffset.UnixEpoch);

        var entries = new CodeReviewService().GetEntries(report);

        Assert.HasCount(2, entries);
        Assert.AreEqual(ErrorSeverity.Error, entries[0].Severity);
        Assert.AreEqual(@"C:\project\source.cs", entries[0].FileName);
        Assert.AreEqual(12, entries[0].LineNumber);
        Assert.AreSame(error, entries[0].Tag);
        Assert.AreEqual(ErrorSeverity.Information, entries[1].Severity);
        Assert.AreSame(manual, entries[1].Tag);
    }
}
