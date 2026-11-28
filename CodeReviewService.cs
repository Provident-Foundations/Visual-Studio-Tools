using Provident.Foundations.VisualStudio.Analysis.CodeReview;
using Provident.Foundations.VisualStudio.Model;
using Provident.Foundations.Wpf.Controls.ErrorList;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VS.Toolset.UI;

/// <summary>
/// Runs code reviews and prepares review results for display and export.
/// </summary>
internal sealed class CodeReviewService
{
    /// <summary>
    /// Creates the default rules file if necessary and opens it for editing.
    /// </summary>
    /// <param name="directory">
    /// The workspace directory.
    /// </param>
    /// <returns>
    /// The path of the rules file.
    /// </returns>
    public async Task<string> EditRulesAsync(string directory)
    {
        string path = await CodeReviewSettingsStore.CreateDefaultFileAsync(directory).ConfigureAwait(false);
        if (Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }) is null)
            throw new InvalidOperationException("No application is associated with JSON files.");
        return path;
    }

    /// <summary>
    /// Runs all review categories or a selected category.
    /// </summary>
    /// <param name="directory">
    /// The workspace directory.
    /// </param>
    /// <param name="solution">
    /// The solution being reviewed.
    /// </param>
    /// <param name="categoryName">
    /// A review category name, or <c>All</c> for all categories.
    /// </param>
    /// <param name="progress">
    /// Receives review progress updates.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancels the review.
    /// </param>
    /// <returns>
    /// The completed review report.
    /// </returns>
    public async Task<ReviewReport> RunAsync(string directory, VsSolutionModel solution, string? categoryName, IProgress<ReviewProgress> progress, CancellationToken cancellationToken)
    {
        CodeReviewAnalyzer analyzer = new();
        if (categoryName == "All")
            return await analyzer.ReviewAllAsync(directory, progress, cancellationToken, solution).ConfigureAwait(false);
        if (!Enum.TryParse(categoryName, out ReviewCategory category) || !Enum.IsDefined(category))
            throw new ArgumentException("Unknown review category.", nameof(categoryName));
        return await analyzer.ReviewCategoryAsync(directory, category, progress, cancellationToken, solution).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts review findings into error-list entries.
    /// </summary>
    /// <param name="report">
    /// The review report to convert.
    /// </param>
    /// <returns>
    /// The corresponding error-list entries.
    /// </returns>
    public ObservableCollection<ErrorEntry> GetEntries(ReviewReport report)
    {
        return new ObservableCollection<ErrorEntry>(report.Findings.Select(finding => new ErrorEntry(
            finding.IsManualReview ? ErrorSeverity.Information : finding.Severity switch
            {
                ReviewSeverity.Error => ErrorSeverity.Error,
                ReviewSeverity.Information => ErrorSeverity.Information,
                _ => ErrorSeverity.Warning,
            },
            $"[{finding.Category}] {finding.RuleName}: {finding.Message}",
            finding.File is null ? finding.Path ?? string.Empty :
                string.IsNullOrEmpty(finding.Path) ? finding.File : Path.Combine(finding.Path, finding.File),
            finding.Line, string.Empty, report.CompletedAt.LocalDateTime) { Tag = finding, LineNumber = finding.Line }));
    }

    /// <summary>
    /// Writes a review report as formatted JSON.
    /// </summary>
    /// <param name="report">
    /// The report to save.
    /// </param>
    /// <param name="fileName">
    /// The destination file path.
    /// </param>
    /// <returns>
    /// A task representing the file write.
    /// </returns>
    public Task SaveAsync(ReviewReport report, string fileName)
    {
        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        });
        return File.WriteAllTextAsync(fileName, json);
    }
}
