using Provident.Foundations.VisualStudio.Analysis.Naming;
using System.Text.Json;
using VS.Toolset.UI;

namespace VisualStudioToolset.Tests;

/// <summary>
/// Verifies naming report formatting.
/// </summary>
[TestClass]
public sealed class NamingAnalysisServiceTests
{
    /// <summary>
    /// Verifies that JSON formatting preserves the result target and counts.
    /// </summary>
    [TestMethod]
    public void FormatReportProducesJsonWithTargetAndStatistics()
    {
        using NamingSolutionAnalysisResult result = new()
        {
            Target = new NamingAnalysisTarget { Name = "Example" },
            Statistics = new NamingStatistics { IdentifierCount = 42 }
        };
        NamingAnalysisService service = new();

        string content = service.FormatReport(result, csv: false);
        using JsonDocument document = JsonDocument.Parse(content);
        Assert.AreEqual("Example", document.RootElement.GetProperty("target").GetProperty("name").GetString());
        Assert.AreEqual(42, document.RootElement.GetProperty("statistics").GetProperty("identifierCount").GetInt32());
    }

    /// <summary>
    /// Verifies the CSV column layout for a report without violations.
    /// </summary>
    [TestMethod]
    public void FormatReportProducesCsvHeader()
    {
        using NamingSolutionAnalysisResult result = new();
        NamingAnalysisService service = new();

        string content = service.FormatReport(result, csv: true);

        Assert.AreEqual("Project,Name,ExpectedStyle,ActualStyle,Severity,Confidence,File,Line,Message" + Environment.NewLine, content);
    }
}
