using Microsoft.Extensions.Logging;
using Provident.Foundations.VisualStudio.Model;
using Provident.Foundations.VisualStudio.Xaml.Validation;
using Provident.Foundations.Wpf.Controls.ErrorList;
using System.Collections.ObjectModel;

namespace VS.Toolset.UI;

/// <summary>
/// Validates XAML style identifiers and converts diagnostics for the error list.
/// </summary>
internal sealed class XamlValidationService
{
    /// <summary>
    /// Validates XAML style identifiers in a solution.
    /// </summary>
    /// <param name="solution">
    /// The solution to validate.
    /// </param>
    /// <param name="progress">
    /// Receives validation status updates.
    /// </param>
    /// <returns>
    /// The validation diagnostics for display in the error list.
    /// </returns>
    public async Task<ObservableCollection<ErrorEntry>> ValidateAsync(VsSolutionModel solution, IProgress<string> progress)
    {
        using XamlStyleIdValidator validator = new();
        validator.StatusUpdate += (_, status) => progress.Report(status);
        var result = await validator.ValidateXamlSolutionAsync(solution).ConfigureAwait(false);
        ObservableCollection<ErrorEntry> entries = [];
        foreach (var project in result.CombinedResultsList)
        {
            foreach (var log in project.Log.Entries)
            {
                ErrorEntry entry = new(GetSeverity(log.Level), log.Message ?? string.Empty,
                    log.File ?? string.Empty, log.LineNumber, log.ParentReference ?? string.Empty, log.LogDateTime);
                entry.Tag = log;
                entries.Add(entry);
            }
        }
        return entries;
    }

    /// <summary>
    /// Maps a validation log level to an error-list severity.
    /// </summary>
    /// <param name="level">
    /// The validation log level.
    /// </param>
    /// <returns>
    /// The corresponding error-list severity.
    /// </returns>
    private static ErrorSeverity GetSeverity(LogLevel level)
    {
        return level switch
        {
            LogLevel.Error or LogLevel.Critical => ErrorSeverity.Error,
            LogLevel.Debug or LogLevel.Information => ErrorSeverity.Information,
            LogLevel.Trace => ErrorSeverity.Log,
            LogLevel.Warning => ErrorSeverity.Warning,
            _ => ErrorSeverity.None
        };
    }
}
