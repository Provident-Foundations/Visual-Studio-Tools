using CommunityToolkit.Mvvm.ComponentModel;
using Provident.Foundations.VisualStudio.Analysis.Naming;
using System.Collections;
using System.ComponentModel;
using System.Windows.Data;

namespace VS.Toolset.UI;

/// <summary>A labeled filter choice; a null value represents All.</summary>
public sealed record NamingReportFilterOption<T>(string Label, T Value);

/// <summary>Visible and total rows for a report section.</summary>
public sealed record NamingReportSectionState(int VisibleCount, int TotalCount)
{
    /// <summary>There are no rows in the result for this section.</summary>
    public bool IsEmpty => TotalCount == 0;
    /// <summary>Rows exist, but none match the current filters.</summary>
    public bool IsNoMatch => TotalCount > 0 && VisibleCount == 0;
}

/// <summary>A distribution category and its count for a report chart.</summary>
public sealed record NamingReportChartValue(string Label, int Count, int Maximum);

/// <summary>
/// Provides filterable, read-only views of a naming analysis result. Treat the result as a
/// snapshot; call SetResult again after changing its data. Null filter values mean all.
/// Bind combo boxes to *Options, DisplayMemberPath to Label, SelectedValuePath to Value,
/// and SelectedValue to the corresponding *Filter; the first choice is All. ResetFilters
/// clears all filters. Project names are case-insensitive and group identically named projects.
/// Diagnostics with an unknown project path and patterns with an unscoped dimension remain visible.
/// Observations without a project name are excluded when a project is selected. Violations use
/// their own project name, falling back to their related observation's project name; if neither
/// has one, they are also excluded. Language, kind and role filter violations only when their
/// ObservationId resolves; unresolved violations remain visible under those dimensions. IDs are
/// matched case-sensitively and the first observation wins when IDs are duplicated. Language and
/// kind also apply to observations and patterns; role also applies to observations. Style applies
/// to observations, patterns and violations (actual style); severity to violations and diagnostics;
/// status only to projects. Unsupported dimensions do not filter a section. Diagnostics with a
/// project path are matched against paths of projects with the selected name.
/// Filters are retained on result replacement, while selections are cleared.
/// </summary>
public sealed class NamingReportViewModel : ObservableObject
{
    private NamingSolutionAnalysisResult? _result;
    private string _searchText = string.Empty;
    private string _search = string.Empty;
    private string? _projectFilter;
    private NamingSourceLanguage? _languageFilter;
    private NamingSymbolKind? _kindFilter;
    private NamingNameRole? _roleFilter;
    private NamingStyle? _styleFilter;
    private NamingViolationSeverity? _severityFilter;
    private NamingAnalysisStatus? _statusFilter;
    private bool _resetting;
    private readonly Dictionary<object, string?[]> _searchValues = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, NamingObservation> _observationsById = new(StringComparer.Ordinal);
    private readonly Dictionary<string, NamingPattern> _patternsById = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _projectPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _knownProjectPaths = new(StringComparer.OrdinalIgnoreCase);
    private NamingViolation? _selectedViolation;
    private NamingObservation? _selectedObservation;
    private NamingPattern? _selectedPattern;
    private NamingSolutionProjectResult? _selectedProject;
    private NamingDiagnostic? _selectedDiagnostic;
    private IReadOnlyList<NamingObservation> _patternObservations = Array.Empty<NamingObservation>();

    public NamingReportViewModel()
    {
        Violations = CreateView(Array.Empty<NamingViolation>());
        Observations = CreateView(Array.Empty<NamingObservation>());
        Patterns = CreateView(Array.Empty<NamingPattern>());
        Projects = CreateView(Array.Empty<NamingSolutionProjectResult>());
        Diagnostics = CreateView(Array.Empty<NamingDiagnostic>());
    }

    public NamingSolutionAnalysisResult? Result => _result;
    public bool HasResult => _result is not null;
    public IReadOnlyList<KeyValuePair<string, int>> TopTokens { get; private set; } = Array.Empty<KeyValuePair<string, int>>();
    public IReadOnlyList<KeyValuePair<string, int>> TopPrefixes { get; private set; } = Array.Empty<KeyValuePair<string, int>>();
    public IReadOnlyList<KeyValuePair<string, int>> TopSuffixes { get; private set; } = Array.Empty<KeyValuePair<string, int>>();
    public IReadOnlyList<NamingReportChartValue> KindChart { get; private set; } = Array.Empty<NamingReportChartValue>();
    public IReadOnlyList<NamingReportChartValue> RoleChart { get; private set; } = Array.Empty<NamingReportChartValue>();
    public IReadOnlyList<NamingReportChartValue> StyleChart { get; private set; } = Array.Empty<NamingReportChartValue>();
    public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value ?? string.Empty)) { _search = _searchText.Trim(); RefreshFilters(); } } }
    public string? ProjectFilter { get => _projectFilter; set { value = string.IsNullOrWhiteSpace(value) ? null : value.Trim(); if (SetProperty(ref _projectFilter, value)) RefreshFilters(); } }
    public NamingSourceLanguage? LanguageFilter { get => _languageFilter; set { if (SetProperty(ref _languageFilter, value)) RefreshFilters(); } }
    public NamingSymbolKind? KindFilter { get => _kindFilter; set { if (SetProperty(ref _kindFilter, value)) RefreshFilters(); } }
    public NamingNameRole? RoleFilter { get => _roleFilter; set { if (SetProperty(ref _roleFilter, value)) RefreshFilters(); } }
    public NamingStyle? StyleFilter { get => _styleFilter; set { if (SetProperty(ref _styleFilter, value)) RefreshFilters(); } }
    /// <summary>Applies the equivalently named severity to violations and diagnostics.</summary>
    public NamingViolationSeverity? SeverityFilter { get => _severityFilter; set { if (SetProperty(ref _severityFilter, value)) RefreshFilters(); } }
    public NamingAnalysisStatus? StatusFilter { get => _statusFilter; set { if (SetProperty(ref _statusFilter, value)) RefreshFilters(); } }

    /// <summary>Project names in the snapshot, plus an All choice.</summary>
    public IReadOnlyList<NamingReportFilterOption<string?>> ProjectOptions { get; private set; } = Array.AsReadOnly(new[] { new NamingReportFilterOption<string?>("All", null) });
    public IReadOnlyList<NamingReportFilterOption<NamingSourceLanguage?>> LanguageOptions { get; } = Options<NamingSourceLanguage>();
    public IReadOnlyList<NamingReportFilterOption<NamingSymbolKind?>> KindOptions { get; } = Options<NamingSymbolKind>();
    public IReadOnlyList<NamingReportFilterOption<NamingNameRole?>> RoleOptions { get; } = Options<NamingNameRole>();
    public IReadOnlyList<NamingReportFilterOption<NamingStyle?>> StyleOptions { get; } = Options<NamingStyle>();
    public IReadOnlyList<NamingReportFilterOption<NamingViolationSeverity?>> SeverityOptions { get; } = Options<NamingViolationSeverity>();
    public IReadOnlyList<NamingReportFilterOption<NamingAnalysisStatus?>> StatusOptions { get; } = Options<NamingAnalysisStatus>();
    private static IReadOnlyList<NamingReportFilterOption<T?>> Options<T>() where T : struct, Enum => Array.AsReadOnly(new[] { new NamingReportFilterOption<T?>("All", null) }.Concat(Enum.GetValues<T>().Select(value => new NamingReportFilterOption<T?>(value.ToString(), value))).ToArray());

    public ICollectionView Violations { get; private set; }
    public ICollectionView Observations { get; private set; }
    public ICollectionView Patterns { get; private set; }
    public ICollectionView Projects { get; private set; }
    public ICollectionView Diagnostics { get; private set; }
    public NamingReportSectionState ViolationsState { get; private set; } = new(0, 0);
    public NamingReportSectionState ObservationsState { get; private set; } = new(0, 0);
    public NamingReportSectionState PatternsState { get; private set; } = new(0, 0);
    public NamingReportSectionState ProjectsState { get; private set; } = new(0, 0);
    public NamingReportSectionState DiagnosticsState { get; private set; } = new(0, 0);

    public NamingViolation? SelectedViolation
    {
        get => _selectedViolation;
        set { if (SetProperty(ref _selectedViolation, value)) { OnPropertyChanged(nameof(RelatedObservation)); OnPropertyChanged(nameof(RelatedPattern)); } }
    }
    public NamingObservation? SelectedObservation { get => _selectedObservation; set => SetProperty(ref _selectedObservation, value); }
    public NamingPattern? SelectedPattern
    {
        get => _selectedPattern;
        set
        {
            if (SetProperty(ref _selectedPattern, value))
            {
                HashSet<string>? ids = value?.ObservationIds.ToHashSet(StringComparer.Ordinal);
                _patternObservations = ids is null || _result is null ? Array.Empty<NamingObservation>() : _result.Observations.Where(item => ids.Contains(item.Id)).ToArray();
                OnPropertyChanged(nameof(PatternObservations));
            }
        }
    }
    public NamingSolutionProjectResult? SelectedProject { get => _selectedProject; set => SetProperty(ref _selectedProject, value); }
    public NamingDiagnostic? SelectedDiagnostic { get => _selectedDiagnostic; set => SetProperty(ref _selectedDiagnostic, value); }
    public NamingObservation? RelatedObservation => !string.IsNullOrWhiteSpace(SelectedViolation?.ObservationId) && _observationsById.TryGetValue(SelectedViolation.ObservationId, out NamingObservation? item) ? item : null;
    public NamingPattern? RelatedPattern => !string.IsNullOrWhiteSpace(SelectedViolation?.PatternId) && _patternsById.TryGetValue(SelectedViolation.PatternId, out NamingPattern? item) ? item : null;
    public IReadOnlyList<NamingObservation> PatternObservations => _patternObservations;

    /// <summary>Replaces the displayed result without disposing it.</summary>
    public void SetResult(NamingSolutionAnalysisResult? result)
    {
        _result = result;
        TopTokens = TopValues(result?.Statistics.CommonTokens);
        TopPrefixes = TopValues(result?.Statistics.CommonPrefixes);
        TopSuffixes = TopValues(result?.Statistics.CommonSuffixes);
        KindChart = BuildChart(result?.Statistics.BySymbolKind);
        RoleChart = BuildChart(result?.Statistics.ByRole);
        StyleChart = BuildChart(result?.Statistics.ByStyle);
        OnPropertyChanged(nameof(TopTokens)); OnPropertyChanged(nameof(TopPrefixes)); OnPropertyChanged(nameof(TopSuffixes));
        OnPropertyChanged(nameof(KindChart)); OnPropertyChanged(nameof(RoleChart)); OnPropertyChanged(nameof(StyleChart));
        SelectedViolation = null; SelectedObservation = null; SelectedPattern = null; SelectedProject = null; SelectedDiagnostic = null;
        _patternObservations = Array.Empty<NamingObservation>();
        _searchValues.Clear(); _observationsById.Clear(); _patternsById.Clear(); _projectPaths.Clear(); _knownProjectPaths.Clear();
        if (result is not null)
        {
            foreach (NamingObservation o in result.Observations) { if (!string.IsNullOrWhiteSpace(o.Id)) _observationsById.TryAdd(o.Id, o); }
            foreach (NamingPattern p in result.Patterns) { if (!string.IsNullOrWhiteSpace(p.Id)) _patternsById.TryAdd(p.Id, p); }
            IEnumerable<NamingProjectIdentity?> identities = result.Projects.Select(p => p.Project)
                .Concat(result.Observations.Select(o => o.SourceFile?.Project)).Concat(result.Violations.Select(v => v.Project));
            foreach (NamingProjectIdentity? project in identities)
            {
                if (!string.IsNullOrWhiteSpace(project?.Name) && !string.IsNullOrWhiteSpace(project.FilePath))
                {
                    if (!_projectPaths.TryGetValue(project.Name, out HashSet<string>? paths)) _projectPaths[project.Name] = paths = new(StringComparer.OrdinalIgnoreCase);
                    paths.Add(project.FilePath);
                    _knownProjectPaths.Add(project.FilePath);
                }
            }
        }
        IEnumerable<string?> names = (result?.Projects.Select(p => p.Project?.Name) ?? []).Concat(result?.Observations.Select(o => o.SourceFile?.Project?.Name) ?? []).Concat(result?.Violations.Select(v => v.Project?.Name) ?? []).Concat(result?.Patterns.Select(p => p.Scope.ProjectName) ?? []);
        ProjectOptions = Array.AsReadOnly(new[] { new NamingReportFilterOption<string?>("All", null) }.Concat(names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).Select(n => new NamingReportFilterOption<string?>(n!, n))).ToArray());
        Violations = CreateView(result?.Violations ?? Array.Empty<NamingViolation>());
        Observations = CreateView(result?.Observations ?? Array.Empty<NamingObservation>());
        Patterns = CreateView(result?.Patterns ?? Array.Empty<NamingPattern>());
        Projects = CreateView(result?.Projects ?? Array.Empty<NamingSolutionProjectResult>());
        Diagnostics = CreateView(result?.Diagnostics ?? Array.Empty<NamingDiagnostic>());
        OnPropertyChanged(nameof(Result)); OnPropertyChanged(nameof(HasResult)); OnPropertyChanged(nameof(ProjectOptions));
        OnPropertyChanged(nameof(Violations)); OnPropertyChanged(nameof(Observations)); OnPropertyChanged(nameof(Patterns)); OnPropertyChanged(nameof(Projects)); OnPropertyChanged(nameof(Diagnostics));
        OnPropertyChanged(nameof(RelatedObservation)); OnPropertyChanged(nameof(RelatedPattern)); OnPropertyChanged(nameof(PatternObservations));
        UpdateStates();
    }

    /// <summary>Clears search and all dimension filters with a single refresh.</summary>
    public void ResetFilters()
    {
        _resetting = true;
        try { SearchText = string.Empty; ProjectFilter = null; LanguageFilter = null; KindFilter = null; RoleFilter = null; StyleFilter = null; SeverityFilter = null; StatusFilter = null; }
        finally { _resetting = false; }
        RefreshFilters();
    }

    private static IReadOnlyList<KeyValuePair<string, int>> TopValues(IReadOnlyDictionary<string, int>? values) =>
        values is null ? Array.Empty<KeyValuePair<string, int>>() : values.OrderByDescending(item => item.Value).ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase).Take(20).ToArray();

    private static IReadOnlyList<NamingReportChartValue> BuildChart<T>(IReadOnlyDictionary<T, int>? values) where T : notnull
    {
        if (values is null || values.Count == 0)
            return Array.Empty<NamingReportChartValue>();
        int maximum = Math.Max(1, values.Values.Max());
        return values.OrderByDescending(item => item.Value).ThenBy(item => item.Key.ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(item => new NamingReportChartValue(item.Key.ToString() ?? string.Empty, item.Value, maximum)).ToArray();
    }

    private ICollectionView CreateView(IEnumerable items)
    {
        List<object> snapshot = items.Cast<object>().ToList();
        foreach (object item in snapshot) _searchValues[item] = SearchValues(item);
        return new ListCollectionView(snapshot) { Filter = Matches };
    }

    private string?[] SearchValues(object item) => item switch
    {
        NamingViolation v => ViolationSearchValues(v),
        NamingObservation o => [o.Name, o.Namespace, o.ContainingType, o.SourceFile?.Path, o.SourceFile?.Project?.Name, o.Kind.ToString(), o.Role.ToString(), o.Language.ToString(), o.Classification.Style.ToString()],
        NamingPattern p => [p.Scope.ProjectName, p.Scope.Namespace, p.Scope.SymbolKind?.ToString(), p.Scope.Language?.ToString(), p.DominantStyle.ToString()],
        NamingSolutionProjectResult p => [p.Project?.Name, p.Project?.FilePath, p.Status.ToString()],
        NamingDiagnostic d => [d.Code, d.Message, d.Detail, d.ProjectPath, d.Location?.FilePath, d.Severity.ToString()],
        _ => []
    };

    private string?[] ViolationSearchValues(NamingViolation violation)
    {
        NamingObservation? observation = null;
        if (!string.IsNullOrWhiteSpace(violation.ObservationId))
            _observationsById.TryGetValue(violation.ObservationId, out observation);
        return [violation.Name, violation.Message, violation.Project?.Name, observation?.SourceFile?.Project?.Name,
            violation.Location?.FilePath, violation.Severity.ToString(), violation.ActualStyle.ToString(), violation.ExpectedStyle.ToString(),
            observation?.Language.ToString(), observation?.Kind.ToString(), observation?.Role.ToString()];
    }

    private bool ProjectMatches(string? name, bool unscoped = false) => ProjectFilter is null || (unscoped && string.IsNullOrWhiteSpace(name)) || string.Equals(ProjectFilter, name, StringComparison.OrdinalIgnoreCase);
    private bool DiagnosticProjectMatches(string? path) => ProjectFilter is null || string.IsNullOrWhiteSpace(path) || !_knownProjectPaths.Contains(path) || (_projectPaths.TryGetValue(ProjectFilter, out HashSet<string>? paths) && paths.Contains(path));

    private bool ViolationMatches(NamingViolation violation)
    {
        NamingObservation? observation = null;
        if (!string.IsNullOrWhiteSpace(violation.ObservationId))
            _observationsById.TryGetValue(violation.ObservationId, out observation);
        string? projectName = violation.Project?.Name;
        if (string.IsNullOrWhiteSpace(projectName))
            projectName = observation?.SourceFile?.Project?.Name;

        return ProjectMatches(projectName)
            && (observation is null || LanguageFilter is null || observation.Language == LanguageFilter)
            && (observation is null || KindFilter is null || observation.Kind == KindFilter)
            && (observation is null || RoleFilter is null || observation.Role == RoleFilter)
            && (StyleFilter is null || violation.ActualStyle == StyleFilter)
            && (SeverityFilter is null || violation.Severity == SeverityFilter);
    }

    private bool Matches(object item)
    {
        if (_search.Length != 0 && !(_searchValues.TryGetValue(item, out string?[]? values) ? values : SearchValues(item)).Any(value => value?.Contains(_search, StringComparison.OrdinalIgnoreCase) == true)) return false;
        return item switch
        {
            NamingViolation v => ViolationMatches(v),
            NamingObservation o => ProjectMatches(o.SourceFile?.Project?.Name) && (LanguageFilter is null || o.Language == LanguageFilter) && (KindFilter is null || o.Kind == KindFilter) && (RoleFilter is null || o.Role == RoleFilter) && (StyleFilter is null || o.Classification.Style == StyleFilter),
            NamingPattern p => ProjectMatches(p.Scope.ProjectName, true) && (LanguageFilter is null || p.Scope.Language is null || p.Scope.Language == LanguageFilter) && (KindFilter is null || p.Scope.SymbolKind is null || p.Scope.SymbolKind == KindFilter) && (StyleFilter is null || p.DominantStyle == StyleFilter),
            NamingSolutionProjectResult p => ProjectMatches(p.Project?.Name) && (StatusFilter is null || p.Status == StatusFilter),
            NamingDiagnostic d => DiagnosticProjectMatches(d.ProjectPath) && (SeverityFilter is null || (d.Severity, SeverityFilter) is (NamingDiagnosticSeverity.Information, NamingViolationSeverity.Information) or (NamingDiagnosticSeverity.Warning, NamingViolationSeverity.Warning) or (NamingDiagnosticSeverity.Error, NamingViolationSeverity.Error)),
            _ => true
        };
    }
    private void RefreshFilters()
    {
        if (_resetting) return;
        Violations.Refresh(); Observations.Refresh(); Patterns.Refresh(); Projects.Refresh(); Diagnostics.Refresh();
        UpdateStates();
    }
    private void UpdateStates()
    {
        ViolationsState = new(((ListCollectionView)Violations).Count, _result?.Violations.Count ?? 0);
        ObservationsState = new(((ListCollectionView)Observations).Count, _result?.Observations.Count ?? 0);
        PatternsState = new(((ListCollectionView)Patterns).Count, _result?.Patterns.Count ?? 0);
        ProjectsState = new(((ListCollectionView)Projects).Count, _result?.Projects.Count ?? 0);
        DiagnosticsState = new(((ListCollectionView)Diagnostics).Count, _result?.Diagnostics.Count ?? 0);
        OnPropertyChanged(nameof(ViolationsState)); OnPropertyChanged(nameof(ObservationsState)); OnPropertyChanged(nameof(PatternsState)); OnPropertyChanged(nameof(ProjectsState)); OnPropertyChanged(nameof(DiagnosticsState)); OnPropertyChanged(nameof(Counts)); OnPropertyChanged(nameof(ActiveFilters));
    }
    /// <summary>Gets a readable summary of the active report filters.</summary>
    public string ActiveFilters
    {
        get
        {
            List<string> filters = [];
            if (_search.Length > 0) filters.Add($"Search: {_search}");
            if (ProjectFilter is not null) filters.Add($"Project: {ProjectFilter}");
            if (LanguageFilter is not null) filters.Add($"Language: {LanguageFilter}");
            if (KindFilter is not null) filters.Add($"Kind: {KindFilter}");
            if (RoleFilter is not null) filters.Add($"Role: {RoleFilter}");
            if (StyleFilter is not null) filters.Add($"Style: {StyleFilter}");
            if (SeverityFilter is not null) filters.Add($"Severity: {SeverityFilter}");
            if (StatusFilter is not null) filters.Add($"Project status: {StatusFilter}");
            return filters.Count == 0 ? "No active filters" : "Active filters: " + string.Join(" | ", filters);
        }
    }
    /// <summary>Gets the visible and total counts for each report list.</summary>
    public string Counts => $"Violations {ViolationsState.VisibleCount}/{ViolationsState.TotalCount}  |  Names {ObservationsState.VisibleCount}/{ObservationsState.TotalCount}  |  Patterns {PatternsState.VisibleCount}/{PatternsState.TotalCount}  |  Projects {ProjectsState.VisibleCount}/{ProjectsState.TotalCount}  |  Diagnostics {DiagnosticsState.VisibleCount}/{DiagnosticsState.TotalCount}";
}
