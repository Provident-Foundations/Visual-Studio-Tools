using Provident.Foundations.VisualStudio.Analysis.Naming;
using Provident.Foundations.VisualStudio.Model;

namespace VS.Toolset.UI;

/// <summary>
/// Runs naming analysis for a solution or an individual project.
/// </summary>
internal sealed class NamingAnalysisService
{
    /// <summary>
    /// Analyzes the solution or selected project.
    /// </summary>
    /// <param name="solution">
    /// The solution containing the projects to analyze.
    /// </param>
    /// <param name="selectedProject">
    /// The project to analyze, or <see langword="null"/> to analyze the solution.
    /// </param>
    /// <param name="configuration">
    /// The configuration captured for this run.
    /// </param>
    /// <param name="progress">
    /// Receives analysis progress updates.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancels the analysis.
    /// </param>
    /// <returns>
    /// The analysis result; the caller owns the returned result.
    /// </returns>
    public async Task<NamingSolutionAnalysisResult> AnalyzeAsync(VsSolutionModel solution, SolutionProjectModel? selectedProject, NamingAnalysisConfiguration configuration, IProgress<NamingAnalysisProgress> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (selectedProject is null)
        {
            using NamingSolutionAnalyzer analyzer = new();
            return await analyzer.AnalyzeAsync(solution, configuration, progress: progress, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        ProjectModel project = selectedProject.Project ?? throw new InvalidOperationException("The selected project is no longer loaded; select a loaded project before analyzing it.");
        using NamingProjectAnalyzer projectAnalyzer = new();
        NamingProjectAnalysisResult projectResult = await projectAnalyzer.AnalyzeAsync(
            project, NamingProjectIdentity.From(selectedProject), configuration, progress: progress, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new NamingSolutionAnalysisResult
        {
            Target = projectResult.Target,
            Projects = new[] { new NamingSolutionProjectResult { Project = projectResult.Project, Result = projectResult, Status = projectResult.Status, Diagnostics = projectResult.Diagnostics, Dependencies = project.ProjectReferences.Select(reference => reference.Include ?? string.Empty).Where(value => value.Length > 0).ToList() } },
            Observations = projectResult.Observations,
            Patterns = projectResult.Patterns,
            Violations = projectResult.Violations,
            Statistics = projectResult.Statistics,
            Index = projectResult.Index,
            Diagnostics = projectResult.Diagnostics,
            Status = projectResult.Status,
            Duration = projectResult.Duration
        };
    }

    /// <summary>
    /// Formats a naming analysis result as JSON or a CSV violations report.
    /// </summary>
    /// <param name="result">
    /// The result to format.
    /// </param>
    /// <param name="csv">
    /// <see langword="true"/> for CSV; otherwise, JSON.
    /// </param>
    /// <returns>
    /// The formatted report content.
    /// </returns>
    public string FormatReport(NamingSolutionAnalysisResult result, bool csv)
    {
        using NamingAnalysisReport report = NamingAnalysisReport.From(result);
        return csv ? NamingReportWriter.ViolationsToCsv(report) : NamingReportWriter.ToJson(report);
    }
}
