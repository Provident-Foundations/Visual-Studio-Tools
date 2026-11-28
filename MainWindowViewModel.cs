using CommunityToolkit.Mvvm.Input;
using Microsoft.CodeAnalysis;
using Provident.Foundations.SqlMaker;
using Provident.Foundations.VisualStudio.Analysis.CodeReview;
using Provident.Foundations.VisualStudio.Analysis.Naming;
using Provident.Foundations.VisualStudio.Model;
using Provident.Foundations.Wpf.Controls;
using Provident.Foundations.Wpf.Controls.ErrorList;
using Provident.Foundations.Wpf.Controls.Mvvm;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Visual.Studio.Toolset.UI.Menu;
using VS.Toolset.UI.Dialogs;

namespace VS.Toolset.UI;

/// <summary>
/// Provides the view model implementation for the central application dialog.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase, IMainMenuViewModel
{
    #region Private Member Declaration

    #region Main Window and Window References
    /// <summary>
    /// The reference to the main dialog.
    /// </summary>
    private MainWindow? _window;

    /// <summary>
    /// Manages the open file editing windows.
    /// </summary>
    private readonly EditorWindowManager _editorWindows = new();

    /// <summary>
    /// The dialog provider service.
    /// </summary>
    private DialogService? _dialogService;
    #endregion

    #region Solutions And Project References
    /// <summary>
    /// The open solution instance.
    /// </summary>
    private VsSolutionModel? _vsSolution;

    /// <summary>
    /// Loads solution and project workspaces.
    /// </summary>
    private readonly WorkspaceService _workspaceService = new();
    /// <summary>
    /// Runs naming analyses and formats their reports.
    /// </summary>
    private readonly NamingAnalysisService _namingService = new();
    /// <summary>
    /// Validates XAML style identifiers.
    /// </summary>
    private readonly XamlValidationService _validationService = new();
    /// <summary>
    /// Runs and exports code reviews.
    /// </summary>
    private readonly CodeReviewService _reviewService = new();
    /// <summary>
    /// Applies and saves theme selections.
    /// </summary>
    private readonly ThemeService _themeService = new();
    /// <summary>
    /// Tracks the active cancellable analysis operation.
    /// </summary>
    private readonly AnalysisOperationCoordinator _analysisOperation = new();

    /// <summary>
    /// The open SQL Maker project.
    /// </summary>
    private SqlDataProject? _sqlProject;
    #endregion

    #region User Control Data Sources
    /// <summary>
    /// The list of VS code errors to be displayed.
    /// </summary>
    private ObservableCollection<ErrorEntry>? _errorList;
    /// <summary>
    /// The result of the most recently completed naming analysis.
    /// </summary>
    private NamingSolutionAnalysisResult? _namingResult;
    private bool _isNamingReportRunning;
    private string _namingReportStatus = "Run a naming analysis to view results.";
    private NamingAnalysisConfiguration _namingConfiguration = NamingAnalysisConfiguration.Default;
    private SolutionProjectModel? _lastNamingProject;
    private string _lastNamingSettings = "Run settings unavailable.";
    /// <summary>
    /// The latest code review report.
    /// </summary>
    private ReviewReport? _reviewReport;
    #endregion

    #region State Values
    /// <summary>
    /// Indicates whether the view model is processing an general operation.
    /// </summary>
    private bool _isBusy;

    /// <summary>
    /// Indicates whether the view model is processing an business operation.
    /// </summary>
    private bool _isBusyWithStatus;

    /// <summary>
    /// Indicates whether the text-to-hex tool is visible.
    /// </summary>
    private bool _isTextToHexVisible;

    /// <summary>
    /// Describes the application's current status.
    /// </summary>
    private string _status = "Ready";

    /// <summary>
    /// The application path.
    /// </summary>
    private string? _appPath;
    #endregion

    #region Commands

    // Window Commands
    
    /// <summary>
    /// The Close Window / application command.
    /// </summary>
    private IRelayCommand? _closeWindowCommand;
    /// <summary>
    /// The minimize window command.
    /// </summary>
    private IRelayCommand? _minCommand;
    /// <summary>
    /// The maximize window command.
    /// </summary>
    private IRelayCommand? _maxCommand;

    // File Menu Commands

    /// <summary>
    /// The open VS solution command.
    /// </summary>
    private IRelayCommand? _openSolutionCommand;
    /// <summary>
    /// Opens a Visual Studio project.
    /// </summary>
    private IRelayCommand? _openProjectCommand;
    /// <summary>
    /// Creates a new SQL project.
    /// </summary>
    private IRelayCommand? _createSqlCommand;
    /// <summary>
    /// The open SQL Project command.
    /// </summary>
    private IRelayCommand? _openSqlCommand;
    /// <summary>
    /// The close solution command.
    /// </summary>
    private IRelayCommand? _closeSolutionCommand;
    /// <summary>
    /// The close SQL Project command.
    /// </summary>
    private IRelayCommand? _closeSqlCommand;
    /// <summary>
    /// The close any open document command.
    /// </summary>
    private IRelayCommand? _closeDocumentCommand;
    /// <summary>
    /// The save current document command.
    /// </summary>
    private IRelayCommand? _saveCommand;
    /// <summary>
    /// The save all open documents command.
    /// </summary>
    private IRelayCommand? _saveAllCommand;
    /// <summary>
    /// The exit command.
    /// </summary>
    private IRelayCommand? _exitCommand;

    // Tool Menu Commands

    /// <summary>
    /// The show text to hex conversion window command.
    /// </summary>
    private IRelayCommand? _textToHexCommand;
    /// <summary>
    /// The show hext to text conversion window command.
    /// </summary>
    private IRelayCommand? _hextToTextCommand;
    /// <summary>
    /// The show color editor window command.
    /// </summary>
    private IRelayCommand? _colorWindowCommand;
    /// <summary>
    /// The show generate quick hash window command.
    /// </summary>
    private IRelayCommand? _quickHashCommand;

    // Code Analysis and Review Commands

    /// <summary>
    /// Validates XAML style identifier keys in the open solution.
    /// </summary>
    private IRelayCommand? _validateStyleIdsCommand;
    /// <summary>
    /// Starts a solution-wide naming analysis.
    /// </summary>
    private IRelayCommand? _namingAnalyzerCommand;
    /// <summary>
    /// Starts a naming analysis for the selected project.
    /// </summary>
    private IRelayCommand? _projectNamingCommand;
    private IRelayCommand? _rerunNamingCommand;
    private IRelayCommand? _namingSettingsCommand;
    /// <summary>
    /// Cancels the active naming analysis.
    /// </summary>
    private IRelayCommand? _cancelNamingCommand;
    /// <summary>
    /// Exports the naming analysis results.
    /// </summary>
    private IRelayCommand? _exportNamingCommand;
    /// <summary>
    /// Runs a code review.
    /// </summary>
    private IRelayCommand<string>? _reviewCommand;
    /// <summary>
    /// The change themes command.
    /// </summary>
    private IRelayCommand<string>? _changeThemeCommand;
    /// <summary>
    /// The edit code review rules command.
    /// </summary>
    private IRelayCommand? _editReviewRulesCommand;
    /// <summary>
    /// The export code review results command.
    /// </summary>
    private IRelayCommand? _exportReviewCommand;
    #endregion

    #endregion

    #region Constructor / Dispose Methods
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="parentWindow">
    /// The parent to the <see cref="MainWindow"/> instance to which the view model is bound.
    /// </param>
    public MainWindowViewModel(MainWindow parentWindow)
    {
        InitializeDialogServices(parentWindow);
        InitializeProjectReferences();
        InitializeUserControlData();
        InitializeCommandInstances();
        InitializeStateValues();
    }
    /// <summary>
    /// Cleans up the resources used by the view model.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release managed resources; otherwise, <see langword="false"/>.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        _window = null;
        if (disposing)
        {
            _analysisOperation.Cancel();
            NamingSolutionAnalysisResult? previousResult = NamingResult;
            NamingResult = null;
            previousResult?.Dispose();
            _errorList?.Clear();
            _editorWindows.Dispose();
            _dialogService?.Dispose();
        }

        _status = string.Empty;
        _appPath = null;
        _closeWindowCommand = null;
        _closeSqlCommand = null;
        _closeDocumentCommand = null;
        _minCommand = null;
        _maxCommand = null;
        _openSolutionCommand = null;
        _openProjectCommand = null;
        _createSqlCommand = null;
        _openSqlCommand = null;
        _closeSolutionCommand = null;
        _textToHexCommand = null;
        _hextToTextCommand = null;
        _colorWindowCommand = null;
        _quickHashCommand = null;
        _validateStyleIdsCommand = null;
        _namingAnalyzerCommand = null;
        _projectNamingCommand = null;
        _rerunNamingCommand = null;
        _namingSettingsCommand = null;
        _lastNamingProject = null;
        _cancelNamingCommand = null;
        _exportNamingCommand = null;
        _dialogService = null;

        base.Dispose(disposing);
    }
    #endregion

    #region Public Properties

    #region Data Properties
    /// <summary>
    /// Gets the reference to the list of open edit windows.
    /// </summary>
    /// <value>
    /// An <see cref="ObservableCollection{T}"/> of <see cref="FileEditorWindow"/> instances containing the
    /// open edit windows.
    /// </value>
    public ObservableCollection<FileEditorWindow> EditWindows => _editorWindows.Windows;

    /// <summary>
    /// Gets the validation and diagnostic entries displayed by the error list.
    /// </summary>
    /// <value>
    /// An <see cref="ObservableCollection{T}"/> of <see cref="ErrorEntry"/> instances containing the
    /// error entries to be displayed.
    /// </value>
    public ObservableCollection<ErrorEntry> Errors
    {
        get => _errorList ?? [];
        set
        {
            SetProperty(ref _errorList, value);
        }
    }

    /// <summary>
    /// Gets the solution displayed by the solution explorer.
    /// </summary>
    /// <value>
    /// The loaded solution, or <see langword="null"/> if no solution is open.
    /// </value>
    public VsSolutionModel? Solution
    {
        get => _vsSolution;
        private set
        {
            if (_vsSolution != value)
            {
                if (SetProperty(ref _vsSolution, value))
                {
                    // Clear the old data when changing solutions.
                    NamingSolutionAnalysisResult? previousResult = NamingResult;
                    NamingResult = null;
                    previousResult?.Dispose();
                    _lastNamingProject = null;
                    LastNamingSettings = "Run settings unavailable.";
                    NamingReportStatus = "Run a naming analysis to view results.";
                    // _reviewReport?.Dispose();
                    _errorList?.Clear();

                    _errorList = [];
                    _reviewReport = null;
                    Errors = [];

                    OnPropertyChanged(nameof(NamingWindowVisibility));
                    OnPropertyChanged(nameof(IsSolutionOpen));
                    OnPropertyChanged(nameof(SolutionIsOpenVisibility));
                    OnPropertyChanged(nameof(DockingAreaVisibility));

                    _namingAnalyzerCommand?.NotifyCanExecuteChanged();
                    _projectNamingCommand?.NotifyCanExecuteChanged();
                    _reviewCommand?.NotifyCanExecuteChanged();
                    _editReviewRulesCommand?.NotifyCanExecuteChanged();
                    _exportReviewCommand?.NotifyCanExecuteChanged();
                }
            }
        }
    }

    /// <summary>
    /// Gets the latest completed naming analysis result.
    /// </summary>
    /// <value>
    /// The analysis result, or <see langword="null"/> if no result is available.
    /// </value>
    public NamingSolutionAnalysisResult? NamingResult
    {
        get => _namingResult;
        private set
        {
            if (SetProperty(ref _namingResult, value))
            {
                _exportNamingCommand?.NotifyCanExecuteChanged();
                _rerunNamingCommand?.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(NamingWindowVisibility));
            }
        }
    }

    /// <summary>Gets the settings used for the displayed result.</summary>
    public string LastNamingSettings
    {
        get => _lastNamingSettings;
        private set => SetProperty(ref _lastNamingSettings, value);
    }

    /// <summary>
    /// Gets a value indicating whether no operation is currently running.
    /// </summary>
    public bool IsIdle => !IsBusyNew && !IsBusyWithStatus;

    /// <summary>
    /// Gets a value indicating whether a naming analysis or code review is active.
    /// </summary>
    public bool IsNamingAnalysisRunning => _analysisOperation.IsActive;

    /// <summary>Gets whether a naming analysis is currently running.</summary>
    public bool IsNamingReportRunning
    {
        get => _isNamingReportRunning;
        private set => SetProperty(ref _isNamingReportRunning, value);
    }

    /// <summary>Gets the latest naming analysis progress or outcome.</summary>
    public string NamingReportStatus
    {
        get => _namingReportStatus;
        private set => SetProperty(ref _namingReportStatus, value);
    }
    #endregion

    #region Commands

    #region Window Commands
    /// <summary>
    /// Gets the command that closes the main window.
    /// </summary>
    /// <value>
    /// An <see cref="IRelayCommand"/> instance, or <b>null</b>.
    /// </value>
    public IRelayCommand? CloseCommand => _closeWindowCommand;

    /// Gets the command that minimizes the main window.
    /// </summary>
    /// <value>
    /// An <see cref="IRelayCommand"/> instance, or <b>null</b>.
    /// </value>
    public IRelayCommand? MinimizeCommand => _minCommand;

    /// <summary>
    /// Gets the command that maximizes or restores the main window.
    /// </summary>
    /// <value>
    /// An <see cref="IRelayCommand"/> instance, or <b>null</b>.
    /// </value>
    public IRelayCommand? MaximizeCommand => _maxCommand;
    #endregion

    #region Main Menu Commands

    #region File Menu

    #region Visual Studio Solutions And Projects
    /// <summary>
    /// Gets the reference to the command used to close an open Visual Studio project.
    /// </summary>
    public IRelayCommand CloseProjectCommand { get; set; }
    /// <summary>
    /// Gets the reference to the command used to open a Visual Studio project.
    /// </summary>
    public IRelayCommand OpenProjectCommand { get; set; }
    /// <summary>
    /// Gets the reference to the command used to close an open Visual Studio solution.
    /// </summary>
    public IRelayCommand CloseSolutionCommand { get; set; }
    /// <summary>
    /// Gets the reference to the command used to open a Visual Studio solution.
    /// </summary>
    public IRelayCommand OpenSolutionCommand { get; set; }
    #endregion

    #region SQL Maker Projects

    /// <summary>
    /// Gets the reference to the command used to create a new SQL Maker project.
    /// </summary>
    public IRelayCommand CreateNewSqlMakerProjectCommand => _createSqlCommand;

    /// <summary>
    /// Gets the reference to the command used to close a SQL Maker project.
    /// </summary>
    public IRelayCommand CloseSqlMakerProjectCommand => _closeSqlCommand;

    /// <summary>
    /// Gets the reference to the command used to open a SQL Maker project.
    /// </summary>
    public IRelayCommand OpenSqlMakerProjectCommand => _openSqlCommand;
    #endregion

    #region General
    /// <summary>
    /// Gets the reference to the command used to indicate the user is exiting the application.
    /// </summary>
    public IRelayCommand ExitApplicationCommand => _exitCommand;

    /// <summary>
    /// Gets the reference to the command used to save the current document.
    /// </summary>
    public IRelayCommand SaveCommand => _saveCommand;

    /// <summary>
    /// Gets the reference to the command used to save all open documents.
    /// </summary>
    public IRelayCommand SaveAllCommand => _saveAllCommand;

    /// <summary>
    /// Gets the command that closes the current document.
    /// </summary>
    /// <value>
    /// An <see cref="IRelayCommand"/> instance, or <b>null</b>.
    /// </value>
    public IRelayCommand CloseCurrentDocumentCommand => _closeDocumentCommand;
    #endregion

    #endregion

    /// <summary>
    /// Gets the command that validates XAML style identifier keys.
    /// </summary>
    /// <value>
    /// The command, or <see langword="null"/> if it has not been initialized.
    /// </value>
    public IRelayCommand? ValidateStyleIdsCommand => _validateStyleIdsCommand;

    /// <summary>
    /// Gets the command that starts a solution-wide naming analysis.
    /// </summary>
    public IRelayCommand? NamingAnalyzerCommand => _namingAnalyzerCommand;

    /// <summary>
    /// Gets the command that analyzes the selected project.
    /// </summary>
    public IRelayCommand? ProjectNamingCommand => _projectNamingCommand;

    /// <summary>Gets the command that repeats the previous analysis scope.</summary>
    public IRelayCommand? RerunNamingCommand => _rerunNamingCommand;
    /// <summary>Gets the command that edits settings for future analyses.</summary>
    public IRelayCommand? NamingSettingsCommand => _namingSettingsCommand;

    /// <summary>
    /// Gets the command that cancels the active naming analysis or code review.
    /// </summary>
    public IRelayCommand? CancelNamingCommand => _cancelNamingCommand;

    /// <summary>
    /// Gets the command that exports the naming analysis report.
    /// </summary>
    public IRelayCommand? ExportNamingCommand => _exportNamingCommand;
    /// <summary>
    /// Gets the command that runs a code review for the selected category.
    /// </summary>
    public IRelayCommand<string>? ReviewCommand => _reviewCommand;

    /// <summary>
    /// Gets the command that changes the application theme.
    /// </summary>
    public IRelayCommand<string>? ChangeThemeCommand => _changeThemeCommand;

    /// <summary>
    /// Gets whether the standard dark theme is active.
    /// </summary>
    public bool IsDarkTheme => VisualThemeManager.CurrentTheme == VisualTheme.Dark;

    /// <summary>
    /// Gets whether the standard light theme is active.
    /// </summary>
    public bool IsLightTheme => VisualThemeManager.CurrentTheme == VisualTheme.Light;

    /// <summary>
    /// Gets whether the Provident dark theme is active.
    /// </summary>
    public bool IsProvidentDarkTheme => VisualThemeManager.CurrentTheme == VisualTheme.ProvidentDark;

    /// <summary>
    /// Gets whether the Provident light theme is active.
    /// </summary>
    public bool IsProvidentLightTheme => VisualThemeManager.CurrentTheme == VisualTheme.ProvidentLight;

    /// <summary>
    /// Gets the command that opens the code review rules file.
    /// </summary>
    public IRelayCommand? EditReviewRulesCommand => _editReviewRulesCommand;

    /// <summary>
    /// Gets the command that exports the code review report.
    /// </summary>
    public IRelayCommand? ExportReviewCommand => _exportReviewCommand;
    #endregion

    #endregion

    /// <summary>
    /// Gets the cursor used while an operation is running.
    /// </summary>
    /// <value>
    /// The wait cursor when busy; otherwise, the arrow cursor.
    /// </value>
    public Cursor CursorState
    {
        get
        {
            if (_isBusy || _isBusyWithStatus)
                return Cursors.Wait;
            else
                return Cursors.Arrow;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the application is busy.
    /// </summary>
    /// <value>
    /// <b>true</b> if the application is busy processing; otherwise, <b>false</b>.
    /// </value>
    public bool IsBusyNew
    {
        get => _isBusy;
        set
        {
            SetProperty(ref _isBusy, value);
            OnPropertyChanged(nameof(BusyVisibility));
            OnPropertyChanged(nameof(CursorState));
            OnPropertyChanged(nameof(IsIdle));
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the application is busy.
    /// </summary>
    /// <value>
    /// <b>true</b> if the application is busy processing; otherwise, <b>false</b>.
    /// </value>
    public bool IsBusyWithStatus
    {
        get => _isBusyWithStatus;
        set
        {
            SetProperty(ref _isBusyWithStatus, value);
            OnPropertyChanged(nameof(BusyWithStatusVisibility));
            OnPropertyChanged(nameof(BusyVisibility));
            OnPropertyChanged(nameof(AnythingIsOpenVisibility));
            OnPropertyChanged(nameof(CursorState));
            OnPropertyChanged(nameof(IsIdle));
            
            _namingAnalyzerCommand?.NotifyCanExecuteChanged();
            _projectNamingCommand?.NotifyCanExecuteChanged();
            _rerunNamingCommand?.NotifyCanExecuteChanged();
            _namingSettingsCommand?.NotifyCanExecuteChanged();
            _exportNamingCommand?.NotifyCanExecuteChanged();
            _reviewCommand?.NotifyCanExecuteChanged();
            _editReviewRulesCommand?.NotifyCanExecuteChanged();
            _exportReviewCommand?.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets the visibility of the busy overlay for either kind of operation.
    /// </summary>
    /// <value>
    /// <see cref="Visibility.Visible"/> while busy; otherwise, <see cref="Visibility.Collapsed"/>.
    /// </value>
    public Visibility BusyVisibility
    {
        get
        {
            if (_isBusy || _isBusyWithStatus)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Gets the visibility enumeration based on the <see cref="IsBusyWithStatus"/> state.
    /// </summary>
    /// <value>
    /// A <see cref="Visibility"/> enumerated value.
    /// </value>
    public Visibility BusyWithStatusVisibility
    {
        get
        {
            if (_isBusyWithStatus)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Gets a value indicating whether a solution is currently open.
    /// </summary>
    /// <value>
    /// <b>true</b> if a solution is open; otherwise, <b>false</b>.
    /// </value>
    public bool IsSolutionOpen
    {
        get => _vsSolution != null;
    }

    /// <summary>
    /// Gets a value indicating whether a SQL Maker project is currently open.
    /// </summary>
    /// <value>
    /// <b>true</b> if a SQL Maker project is open; otherwise, <b>false</b>.
    /// </value>
    public bool IsSqlOpen
    {
        get => _sqlProject != null;
    }

    /// <summary>
    /// Gets or sets the description of the application's current status.
    /// </summary>
    /// <value>
    /// A string containing the description text.
    /// </value>
    public string Status
    {
        get => _status;
        set
        {
            SetProperty(ref _status, value);
        }
    }

    /// <summary>
    /// Gets the visibility of the naming analysis results window.
    /// </summary>
    /// <value>
    /// A <see cref="Visibility"/> enumerated value.
    /// </value>
    public Visibility NamingWindowVisibility
    {
        get
        {
            if (_namingResult is not null)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }
    }
    /// <summary>
    /// Gets whether the solution or SQL project area should be visible.
    /// </summary>
    /// <value>
    /// A <see cref="Visibility"/> enumerated value.
    /// </value>
    public Visibility AnythingIsOpenVisibility
    {
        get
        {
            if (_vsSolution is null && _sqlProject is null)
                return Visibility.Collapsed;
            else
                return Visibility.Visible;
        }
    }
    /// <summary>
    /// Gets the visibility enumeration based on the <see cref="IsSqlOpen"/> state.
    /// </summary>
    /// <value>
    /// A <see cref="Visibility"/> enumerated value.
    /// </value>
    public Visibility SqlOpenVisibility
    {
        get
        {
            if (_sqlProject is null)
                return Visibility.Collapsed;
            else
                return Visibility.Visible;
        }
    }

    /// <summary>
    /// Gets the visibility enumeration based on the <see cref="IsSolutionOpen"/> state.
    /// </summary>
    /// <value>
    /// A <see cref="Visibility"/> enumerated value.
    /// </value>
    public Visibility SolutionIsOpenVisibility
    {
        get
        {
            if (_vsSolution is null)
                return Visibility.Collapsed;
            else
                return Visibility.Visible;
        }
    }

    /// <summary>
    /// Gets the visibility of the docking area.
    /// </summary>
    public Visibility DockingAreaVisibility => _vsSolution is not null || _isTextToHexVisible
        ? Visibility.Visible
        : Visibility.Collapsed;

    public IRelayCommand CloseOpenDocumentCommand { get; }


    /// <summary>
    /// Updates the text-to-hex visibility used by the docking area.
    /// </summary>
    /// <param name="isVisible">
    /// Whether the text-to-hex pane is visible.
    /// </param>
    internal void SetTextToHexVisible(bool isVisible)
    {
        if (SetProperty(ref _isTextToHexVisible, isVisible))
            OnPropertyChanged(nameof(DockingAreaVisibility));
    }
    #endregion

    #region Command Implementations

    #region Window Commands
    /// <summary>
    /// Closes the main window.
    /// </summary>
    private void CloseWindow()
    {
        IsBusyNew = true;
        try
        {
            _window?.Close();
        }
        finally
        {
            IsBusyNew = false;
        }
    }

    /// <summary>
    /// Maximizes the main window.
    /// </summary>
    public void MaximizeWindow()
    {
        IsBusyNew = true;
        if (_window?.WindowState == WindowState.Maximized)
            _window?.WindowState = WindowState.Normal;
        else
            _window?.WindowState = WindowState.Maximized;
        IsBusyNew = false;
    }

    /// <summary>
    /// Minimizes the main window.
    /// </summary>
    public void MinimizeWindow()
    {
        IsBusyNew = true;
        _window?.WindowState = WindowState.Minimized;
        IsBusyNew = false;
    }

    #endregion

    #region File Menu Commands

    #region Visual Studio Solutions / Projects
    /// <summary>
    /// Opens a Visual Studio solution for editing.
    /// </summary>
    private async Task OpenSolutionAsync()
    {
        IsBusyNew = true;
        try
        {
            string? fileName = _dialogService?.ShowOpenSolutionDialog();
            if (!string.IsNullOrEmpty(fileName))
                await OpenWorkspaceAsync(fileName, () => _workspaceService.LoadSolution(fileName));
        }
        finally
        {
            IsBusyNew = false;
        }
    }

    /// <summary>
    /// Opens a Visual Studio project for editing.
    /// </summary>
    private async Task OpenProjectAsync()
    {
        IsBusyNew = true;
        try
        {
            string? fileName = _dialogService?.ShowOpenProjectDialog();
            if (!string.IsNullOrEmpty(fileName))
                await OpenWorkspaceAsync(fileName, () => _workspaceService.LoadProject(fileName));
        }
        finally
        {
            IsBusyNew = false;
        }
    }

    /// <summary>
    /// Closes the currently open solution if no operation is running and all editors can close.
    /// </summary>
    /// <returns>
    /// A task representing the close operation.
    /// </returns>
    public async Task CloseSolution()
    {
        if (!IsIdle)
            return;

        IsBusyNew = true;
        try
        {
            await Task.Yield();
            if (!CloseEditWindows())
            {
                Status = "Close canceled.";
            }
            else
            {
                Solution = null;
                Status = "Ready";
            }
        }
        finally
        {
            IsBusyNew = false;
        }
    }
    #endregion

    #region SQL Maker Projects
    /// <summary>
    /// Displays a dialog for creating an starting a new SQL Maker project.
    /// </summary>
    private async Task CreateSqlProjectAsync()
    {
        IsBusyNew = true;

        // Show the new dialog.
        SqlMakerWindow dialog = new SqlMakerWindow { Owner = _window };
        bool? isOk = dialog.ShowDialog();
        if (isOk is true)
        {
            IsBusyWithStatus = true;
            IsBusyWithStatus = false;
        }
        IsBusyNew = false;

    }
    /// <summary>
    /// Represents the SQL project open operation, which is not yet implemented.
    /// </summary>
    /// <returns>
    /// A completed task.
    /// </returns>
    private async Task OpenSqlProjectAsync()
    {
    }
    /// <summary>
    /// Represents the SQL project close operation, which is not yet implemented.
    /// </summary>
    /// <returns>
    /// A completed task.
    /// </returns>
    private async Task CloseSqlProjectAsync()
    {

    }
    #endregion

    /// <summary>
    /// Exits the application.
    /// </summary>
    private async Task ExitApplicaionAsync()
    {
        if (!CloseEditWindows())
        {
            Status = "Exit canceled.";
            return;
        }
        else
        {
            _window?.Close();
        }
    }
    #endregion

    #region Tool Menu Commands
    /// <summary>
    /// Opens and displays the text-to-hex tool window.
    /// </summary>
    private async Task ShowTextToHexToolAsync()
    {
        IsBusyNew = true;
        _window?.TextToHexPane.Show();
        IsBusyNew = false;
    }

    /// <summary>
    /// Opens and displays the hex-to-text tool window.
    /// </summary>
    private async Task ShowHexToTextToolAsync()
    {
        IsBusyNew = true;
        _window?.HexToTextPane.Show();
        IsBusyNew = false;
    }

    /// <summary>
    /// Opens and displays the color editing tool window.
    /// </summary>
    private async Task ShowColorWindowAsync()
    {
        IsBusyNew = true;
        //_window?.ColorEditPane.Show();
        IsBusyNew = false;
    }

    /// <summary>
    /// Opens and displays the create quick hash tool window.
    /// </summary>
    private async Task ShowQuickHashWindowAsync()
    {
        IsBusyNew = true;
        _window?.QuickHashPane.Show();
        IsBusyNew = false;
    }
    #endregion

    #endregion

    #region Private Methods / Functions
    /// <summary>
    /// Loads a workspace and publishes it if all editor windows can be closed.
    /// </summary>
    /// <param name="fileName">
    /// The solution or project file path.
    /// </param>
    /// <param name="load">
    /// The operation that loads the workspace.
    /// </param>
    /// <returns>
    /// A task representing the load operation.
    /// </returns>
    private async Task OpenWorkspaceAsync(string fileName, Func<VsSolutionModel> load)
    {
        if (IsBusyWithStatus)
            return;
        IsBusyWithStatus = true;
        Status = "Loading...";
        try
        {
            VsSolutionModel loaded = await Task.Run(load);
            if (!CloseEditWindows())
            {
                Status = "Open canceled.";
                return;
            }
            _appPath = Path.GetDirectoryName(fileName);
            Solution = loaded;
            _window?.ShowSolutionExplorer();
            Status = "Ready";
        }
        catch (Exception exception)
        {
            Status = "Unable to open the selected file.";
            MessageBox.Show(_window, exception.Message, "Open failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusyWithStatus = false;
        }
    }

    /// <summary>
    /// Performs a check on the open solution to ensure XAML style ID keys are valid.
    /// </summary>
    public async Task ValidateStyleIds()
    {
        VsSolutionModel? solution = Solution;
        if (solution is null || IsBusyWithStatus)
            return;
        IsBusyWithStatus = true;
        Status = "Reading Solution Contents...";
        bool acceptingProgress = true;
        Progress<string> progress = new(status => { if (acceptingProgress) Status = status; });
        try
        {
            Errors = await Task.Run(() => _validationService.ValidateAsync(solution, progress));
            Status = "Ready";
        }
        catch (Exception exception)
        {
            Status = "Validation failed.";
            MessageBox.Show(_window, exception.Message, "Validation failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            acceptingProgress = false;
            IsBusyWithStatus = false;
        }
    }

    /// <summary>
    /// Starts a naming analysis for the open solution.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous analysis operation.
    /// </returns>
    public Task PerformNamingAnalyisis() => AnalyzeNamingAsync(null);

    private void EditNamingSettings()
    {
        NamingSettingsDialog dialog = new(_namingConfiguration) { Owner = _window };
        if (dialog.ShowDialog() == true)
            _namingConfiguration = dialog.Configuration;
    }

    private Task RerunNamingAnalysis() => AnalyzeNamingAsync(_lastNamingProject);

    /// <summary>
    /// Requests cancellation of the active naming analysis or code review.
    /// </summary>
    private void CancelAnalysis()
    {
        _analysisOperation.Cancel();
        _cancelNamingCommand?.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Starts naming analysis for the selected project, if one is loaded.
    /// </summary>
    /// <returns>
    /// A task representing the analysis operation.
    /// </returns>
    private Task AnalyzeSelectedProject()
    {
        SolutionProjectModel? project = _window?.SelectedNamingProject;
        if (project?.Project is null)
        {
            MessageBox.Show(_window, "Select a loaded project in Solution Explorer first.", "Naming analysis", MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }
        return AnalyzeNamingAsync(project);
    }

    /// <summary>
    /// Runs naming analysis and publishes the completed result.
    /// </summary>
    /// <param name="selectedProject">
    /// The project to analyze, or <see langword="null"/> to analyze the solution.
    /// </param>
    /// <returns>
    /// A task representing the analysis operation.
    /// </returns>
    private async Task AnalyzeNamingAsync(SolutionProjectModel? selectedProject)
    {
        VsSolutionModel? solution = Solution;
        if (solution is null || IsBusyWithStatus)
            return;

        using CancellationTokenSource cancellation = new();
        _analysisOperation.Begin(cancellation);
        IsNamingReportRunning = true;
        OnPropertyChanged(nameof(IsNamingAnalysisRunning));
        _cancelNamingCommand?.NotifyCanExecuteChanged();
        IsBusyWithStatus = true;
        Status = NamingReportStatus = "Starting naming analysis...";
        Progress<NamingAnalysisProgress> progress = new(update =>
        {
            if (_analysisOperation.IsCurrent(cancellation))
                Status = NamingReportStatus = $"{update.Stage}: {update.ProjectName} ({update.CompletedItems}/{update.TotalItems})";
        });

        NamingAnalysisConfiguration configuration = _namingConfiguration;
        try
        {
            NamingSolutionAnalysisResult result = await Task.Run(
                () => _namingService.AnalyzeAsync(solution, selectedProject, configuration, progress, cancellation.Token), cancellation.Token);
            if (cancellation.IsCancellationRequested)
            {
                result.Dispose();
                cancellation.Token.ThrowIfCancellationRequested();
            }
            NamingSolutionAnalysisResult? previousResult = NamingResult;
            NamingResult = result;
            previousResult?.Dispose();
            _lastNamingProject = selectedProject;
            LastNamingSettings = $"Run settings: XAML {(configuration.IncludeXaml ? "included" : "excluded")}; generated files {(configuration.IncludeGeneratedFiles ? "included" : "excluded")}; minimum observations {configuration.MinimumObservationsPerPattern}; dominance {configuration.MinimumDominancePercentage:P0}; violation confidence {configuration.MinimumViolationConfidence:P0}.";
            Status = NamingReportStatus = $"Naming analysis {result.Status}: {result.Statistics.IdentifierCount} names, {result.Violations.Count} violations.";
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            Status = NamingReportStatus = NamingResult is null ? "Naming analysis canceled. No result is available." : "Naming analysis canceled. The previous result is displayed.";
        }
        catch (Exception exception)
        {
            Status = NamingReportStatus = NamingResult is null ? "Naming analysis failed. No result is available." : "Naming analysis failed. The previous result is displayed.";
            MessageBox.Show(_window, exception.Message, "Naming analysis failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _analysisOperation.End(cancellation);
            IsNamingReportRunning = false;
            OnPropertyChanged(nameof(IsNamingAnalysisRunning));
            OnPropertyChanged(nameof(NamingWindowVisibility));
            _cancelNamingCommand?.NotifyCanExecuteChanged();
            IsBusyWithStatus = false;
        }
        OnPropertyChanged(nameof(NamingWindowVisibility));

    }

    /// <summary>
    /// Applies a named theme and saves the selection when possible.
    /// </summary>
    /// <param name="themeName">
    /// The name of the theme to apply.
    /// </param>
    private void ChangeTheme(string? themeName)
    {
        if (!Enum.TryParse(themeName, ignoreCase: true, out VisualTheme theme) || !Enum.IsDefined(theme))
        {
            MessageBox.Show(_window, $"The visual theme '{themeName}' is not supported.", "Theme change failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            _themeService.Apply(theme);
        }
        catch (InvalidOperationException exception)
        {
            Status = "The visual theme could not be changed.";
            MessageBox.Show(_window, exception.Message, "Theme change failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsProvidentDarkTheme));
        OnPropertyChanged(nameof(IsProvidentLightTheme));
        try
        {
            _themeService.Save(theme);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Status = $"{theme} theme selected, but the preference could not be saved.";
            MessageBox.Show(_window, exception.Message, "Theme preference could not be saved", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Status = $"{theme} theme selected.";
    }

    /// <summary>
    /// Opens the code review rules file for editing.
    /// </summary>
    /// <returns>
    /// A task representing the rules-file operation.
    /// </returns>
    private async Task EditReviewRulesAsync()
    {
        if (Solution is null || IsBusyWithStatus || IsBusyNew || string.IsNullOrWhiteSpace(_appPath))
        {
            return;
        }

        try
        {
            string path = await _reviewService.EditRulesAsync(_appPath);
            Status = $"Edit review rules in {path}, then run the review again.";
        }
        catch (Exception exception)
        {
            Status = "Unable to open the code review rules file.";
            MessageBox.Show(_window, exception.Message, "Edit review rules failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Runs a code review and displays the resulting findings.
    /// </summary>
    /// <param name="categoryName">
    /// The review category name, or <c>All</c> for all categories.
    /// </param>
    /// <returns>
    /// A task representing the review operation.
    /// </returns>
    private async Task RunReviewAsync(string? categoryName)
    {
        VsSolutionModel? solution = Solution;
        if (solution is null || IsBusyWithStatus || IsBusyNew || string.IsNullOrEmpty(_appPath))
            return;

        using CancellationTokenSource cancellation = new();
        _analysisOperation.Begin(cancellation);
        OnPropertyChanged(nameof(IsNamingAnalysisRunning));
        _cancelNamingCommand?.NotifyCanExecuteChanged();
        IsBusyWithStatus = true;
        Status = "Starting code review...";
        Progress<ReviewProgress> progress = new(update =>
        {
            if (_analysisOperation.IsCurrent(cancellation))
                Status = $"{update.Message} ({update.Completed}/{update.Total})";
        });
        try
        {
            string directory = _appPath;
            ReviewReport result = await Task.Run(
                () => _reviewService.RunAsync(directory, solution, categoryName, progress, cancellation.Token), cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            _reviewReport = result;
            Errors = _reviewService.GetEntries(result);
            _window?.ShowErrorList();
            Status = $"Automated checks complete: {result.Findings.Count(f => !f.IsManualReview)} advisories; manual review required.";
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            Status = "Code review canceled. Previous results were retained.";
        }
        catch (Exception exception)
        {
            Status = "Code review failed. Previous results were retained.";
            MessageBox.Show(_window, exception.Message, "Code review failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _analysisOperation.End(cancellation);
            OnPropertyChanged(nameof(IsNamingAnalysisRunning));
            _cancelNamingCommand?.NotifyCanExecuteChanged();
            IsBusyWithStatus = false;
        }
    }

    /// <summary>
    /// Prompts for a destination and saves the latest code review report.
    /// </summary>
    /// <returns>
    /// A task representing the export operation.
    /// </returns>
    private async Task ExportReviewAsync()
    {
        ReviewReport? report = _reviewReport;
        if (report is null || IsBusyWithStatus)
            return;
        Microsoft.Win32.SaveFileDialog dialog = new()
        {
            Title = "Save code review",
            Filter = "JSON report (*.json)|*.json",
            DefaultExt = ".json",
            FileName = "code-review.json"
        };
        if (dialog.ShowDialog(_window) != true)
            return;
        IsBusyWithStatus = true;
        Status = "Saving code review...";
        try
        {
            await _reviewService.SaveAsync(report, dialog.FileName);
            Status = $"Code review saved to {dialog.FileName}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or NotSupportedException)
        {
            Status = "Saving code review failed.";
            MessageBox.Show(_window, exception.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusyWithStatus = false;
        }
    }

    /// <summary>
    /// Prompts for a destination and exports the latest naming report.
    /// </summary>
    /// <returns>
    /// A task representing the export operation.
    /// </returns>
    private async Task ExportNamingReportAsync()
    {
        NamingSolutionAnalysisResult? result = NamingResult;
        if (result is null || IsBusyWithStatus)
            return;
        Microsoft.Win32.SaveFileDialog dialog = new()
        {
            Title = "Export naming analysis",
            Filter = "JSON report (*.json)|*.json|Violations CSV (*.csv)|*.csv",
            DefaultExt = ".json",
            FileName = "naming-analysis"
        };
        if (dialog.ShowDialog(_window) != true)
            return;
        string fileName = dialog.FileName;
        bool csv = dialog.FilterIndex == 2;
        IsBusyWithStatus = true;
        Status = "Exporting naming report...";
        try
        {
            string content = await Task.Run(() => _namingService.FormatReport(result, csv));
            await File.WriteAllTextAsync(fileName, content);
            Status = $"Naming report exported to {fileName}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or NotSupportedException)
        {
            Status = "Export failed.";
            MessageBox.Show(_window, exception.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusyWithStatus = false;
        }
    }

    #endregion

    #region Private Methods / Functions

    #region Private Initialization Methods
    /// <summary>
    /// Initializes the references to window objects and services.
    /// </summary>
    /// <param name="parentWindow">
    /// The parent to the <see cref="MainWindow"/> instance to which the view model is bound.
    /// </param>
    private void InitializeDialogServices(MainWindow parentWindow)
    {
        _window = parentWindow;
        _dialogService = new DialogService(parentWindow);
    }

    /// <summary>
    /// Initializes the instances for loading and working with VS and SQL Projects.
    /// </summary>
    private void InitializeProjectReferences()
    {
        _errorList = [];
        _vsSolution = null;
        _sqlProject = null;
    }

    /// <summary>
    /// Initializes the data source instances for the user controls.
    /// </summary>
    private void InitializeUserControlData()
    {
        _errorList = [];
        _namingResult = null;
        _reviewReport = null;
    }

    /// <summary>
    /// Initializes the command objects.
    /// </summary>
    private void InitializeCommandInstances()
    {
        // Window buttons.
        _closeWindowCommand = new RelayCommand(CloseWindow);
        _minCommand = new RelayCommand(MinimizeWindow);
        _maxCommand = new RelayCommand(MaximizeWindow);

        // File Menu - VS 
        _openSolutionCommand = new AsyncRelayCommand(OpenSolutionAsync);
        _openProjectCommand = new AsyncRelayCommand(OpenProjectAsync);
        _closeSolutionCommand = new AsyncRelayCommand(CloseSolution);

        // File Menu - SQL
        _createSqlCommand = new AsyncRelayCommand(CreateSqlProjectAsync);
        _openSqlCommand = new AsyncRelayCommand(OpenSqlProjectAsync);
        _closeSqlCommand = new AsyncRelayCommand(CloseSqlProjectAsync);

        // File Menu
        _exitCommand = new AsyncRelayCommand(ExitApplicaionAsync);

        // Tools Menu.
        _textToHexCommand = new AsyncRelayCommand(ShowTextToHexToolAsync);
        _hextToTextCommand = new AsyncRelayCommand(ShowHexToTextToolAsync);
        _colorWindowCommand = new AsyncRelayCommand(ShowColorWindowAsync);
        _quickHashCommand = new AsyncRelayCommand(ShowQuickHashWindowAsync);


        _validateStyleIdsCommand = new AsyncRelayCommand(ValidateStyleIds);
        _namingAnalyzerCommand = new AsyncRelayCommand(PerformNamingAnalyisis, () => Solution is not null && !IsBusyWithStatus);
        _projectNamingCommand = new AsyncRelayCommand(AnalyzeSelectedProject, () => Solution is not null && !IsBusyWithStatus);
        _rerunNamingCommand = new AsyncRelayCommand(RerunNamingAnalysis, () => NamingResult is not null && Solution is not null && !IsBusyWithStatus);
        _namingSettingsCommand = new RelayCommand(EditNamingSettings, () => !IsBusyWithStatus);
        _cancelNamingCommand = new RelayCommand(CancelAnalysis, () => _analysisOperation.CanCancel);
        _exportNamingCommand = new AsyncRelayCommand(ExportNamingReportAsync, () => NamingResult is not null && !IsBusyWithStatus);
        _reviewCommand = new AsyncRelayCommand<string>(RunReviewAsync, _ => Solution is not null && !IsBusyWithStatus);
        _editReviewRulesCommand = new AsyncRelayCommand(EditReviewRulesAsync, () => Solution is not null && !IsBusyWithStatus);
        _exportReviewCommand = new AsyncRelayCommand(ExportReviewAsync, () => _reviewReport is not null && !IsBusyWithStatus);

        // Theme commands.
        _changeThemeCommand = new RelayCommand<string>(ChangeTheme);

    }

    /// <summary>
    /// Initializes the state values for the dialog.
    /// </summary>
    private void InitializeStateValues()
    {
        _isTextToHexVisible = false;
        _status = "Initializing...";
        _appPath = Environment.ProcessPath;
    }
   
    #endregion

    /// <summary>
    /// Requests that all open editor windows close.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if all editors closed; otherwise, <see langword="false"/>.
    /// </returns>
    internal bool CloseEditWindows()
    {
        return _editorWindows.CloseAll();
    }

    /// <summary>
    /// Opens a source file in an editor or activates its existing editor.
    /// </summary>
    /// <param name="fileName">
    /// The source file path.
    /// </param>
    /// <param name="line">
    /// The line to navigate to, if supplied.
    /// </param>
    /// <param name="projectName">
    /// The associated project name, if available.
    /// </param>
    internal void OpenSource(string fileName, int? line, string? projectName)
    {
        if (_window is not null)
            _editorWindows.Open(_window, fileName, line, projectName);
    }
    #endregion

    #region Public Methods / Functions
    /// <summary>
    /// Determines whether an editor window is open for the specified file.
    /// </summary>
    /// <param name="fileName">
    /// The file name used to identify the editor window.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if an editor window is registered for the file; otherwise, <see langword="false"/>.
    /// </returns>
    public bool HasEditWindow(string fileName)
    {
        return _editorWindows.Contains(fileName);
    }

    /// <summary>
    /// Gets the editor window registered for the specified file.
    /// </summary>
    /// <param name="fileName">
    /// The file name used to identify the editor window.
    /// </param>
    /// <returns>
    /// The registered editor window.
    /// </returns>
    public FileEditorWindow GetEditWindow(string fileName)
    {
        return _editorWindows.Get(fileName);
    }

    /// <summary>
    /// Registers an editor window for a file and tracks its close event.
    /// </summary>
    /// <param name="fileName">
    /// The file name used to identify the editor window.
    /// </param>
    /// <param name="window">
    /// The editor window to register.
    /// </param>
    public void RegisterFileEditWindow(string fileName, FileEditorWindow window)
    {
        _editorWindows.Register(fileName, window);
    }
    #endregion

}