using Provident.Foundations.VisualStudio.Analysis.Naming;
using Provident.Foundations.VisualStudio.Model;
using Provident.Foundations.Wpf.Controls;
using Provident.Foundations.Wpf.Controls.ErrorList;
using Provident.Foundations.Wpf.Controls.SolutionExplorer;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VS.Toolset.UI.Dialogs;
using VS.Toolset.UI.Dialogs.ToolWindowPages.ColorEditor;
using VS.Toolset.UI.Dialogs.ToolWindowPages.HexToText;
using VS.Toolset.UI.Dialogs.ToolWindowPages.QuickHash;
using VS.Toolset.UI.Dialogs.ToolWindowPages.TextToHex;

namespace VS.Toolset.UI;

/// <summary>
/// Provides the central dialog for the application.
/// </summary>
public partial class MainWindow : ProvidentWindowBase
{
    #region Private Member Declarations
    /// <summary>
    /// The view model instance.
    /// </summary>
    private MainWindowViewModel? _vm;

    /// <summary>
    /// The convert text to hexadecimal page view.
    /// </summary>
    private TextToHexPage? _textToHexPage;

    /// <summary>
    /// The convert hexadecimal to text page view.
    /// </summary>
    private HexToTextPage? _hexToTextPage;

    /// <summary>
    /// The color editor page view.
    /// </summary>
    /// </summary>
    private ColorEditorPage? _colorEditPage;

    /// <summary>
    /// The generate hash page view.
    /// </summary>
    private QuickHashPage? _quickhashPage;

    /// <summary>
    /// Controls docked tool-window visibility.
    /// </summary>
    private readonly ToolWindowController _toolWindows = new();
    #endregion

    #region Constructor / Dispose Methods
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> dialog.
    /// </summary>
    /// <remarks>
    /// This is the default constructor.
    /// </remarks>
    public MainWindow()
    {
        InitializeComponent();

        // Set View Model.
        _vm = new MainWindowViewModel(this);
        DataContext = _vm;
        Explorer.DataContext = _vm;
        FileMenu.SetContext(_vm);
        ToolsMenu.SetContext(_vm);

        // Create the sub-pages/views.
        InitializeTextToHexPage();
    }

    /// <summary>
    /// Cleans up resources used by the window.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release managed resources; otherwise, <see langword="false"/>.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (!IsDisposed && disposing)
        {
            RemoveEventHandlers();
            _vm?.Dispose();
        }

        _textToHexPage = null;
        _vm = null;
        DataContext = null;
        base.Dispose(disposing);
    }
    #endregion

    #region Public Properties
    /// <summary>
    /// Gets the reference to the currently selected project for naming analysis.
    /// </summary>
    /// <value>
    /// A <see cref="SolutionProjectModel"/> instance, or <b>null</b>.
    /// </value>
    public SolutionProjectModel? SelectedNamingProject
    {
        get
        {
            return Explorer.SelectedProject;
        }
    }
    #endregion

    #region Protected Method Overrides

    /// <summary>
    /// Prevents closing while work is running or an editor declines to close.
    /// </summary>
    /// <param name="e">
    /// The closing event data.
    /// </param>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (_vm?.IsBusyWithStatus is true)
        {
            e.Cancel = true;
            MessageBox.Show(this, "Wait for the current operation to finish, or cancel the analysis before closing.", "Operation in progress", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else if (_vm is not null && !_vm.CloseEditWindows())
        {
            e.Cancel = true;
        }
        base.OnClosing(e);
    }

    /// <summary>
    /// Releases the window's resources after it closes.
    /// </summary>
    /// <param name="e">
    /// The closed event data.
    /// </param>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Dispose();
    }

    /// <summary>
    /// Assigns event handlers to the control's events.
    /// </summary>
    protected override void AssignEventHandlers()
    {
        // Controls.
        ErrorList.OpenFileRequest += HandleOpenFileRequest;
        Explorer.NodeDoubleClicked += HandleNodeDoubleClicked;
        ErrorListPane.IsVisibleChanged += HandleToolWindowVisibilityChanged;
        SolutionExplorerPane.IsVisibleChanged += HandleToolWindowVisibilityChanged;
        NamingAnalysisPane.IsVisibleChanged += HandleToolWindowVisibilityChanged;

    }

    /// <summary>
    /// Removes event handlers from the control's events.
    /// </summary>
    protected override void RemoveEventHandlers()
    {
        // Controls.
        Explorer.NodeDoubleClicked -= HandleNodeDoubleClicked;
        ErrorList.OpenFileRequest -= HandleOpenFileRequest;
        ErrorListPane.IsVisibleChanged -= HandleToolWindowVisibilityChanged;
        SolutionExplorerPane.IsVisibleChanged -= HandleToolWindowVisibilityChanged;
        NamingAnalysisPane.IsVisibleChanged -= HandleToolWindowVisibilityChanged;
    }
    #endregion

    #region Private Event Handlers

    #region Tools Menu
    /// <summary>
    /// Handles the event when the Text-to-Hexadecimal menu item is clicked.
    /// </summary>
    /// <param name="sender">
    /// The object raising the event.
    /// </param>
    /// <param name="e">
    /// The <see cref="RoutedEventArgs"/> containing the event data.
    /// </param>
    private void HandleTextToHexClicked(object sender, RoutedEventArgs e)
    {
        _toolWindows.Show(TextToHexPane, activate: true);
    }
    #endregion

    #region SQL Menu
    /// <summary>
    /// Handles the event when the SQL -> new menu option is clicked.
    /// </summary>
    /// <param name="sender">
    /// The instance raising the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void HandleNewSqlProjectClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new SqlMakerWindow { Owner = this };
        dialog.ShowDialog();
    }
    /// <summary>
    /// Handles the SQL project open menu event; this operation is not yet implemented.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void HandleOpenSqlProjectClicked(object? sender, RoutedEventArgs e)
    {

    }
    /// <summary>
    /// Handles the stored procedure generation menu event; this operation is not yet implemented.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void HandleGenerateStoredProcsClicked(object? sender, RoutedEventArgs e)
    {
    }
    /// <summary>
    /// Handles the code generation menu event; this operation is not yet implemented.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void HandleGenerateCodeClicked(object? sender, RoutedEventArgs e)
    {
    }
    #endregion

    /// <summary>
    /// Handles the event from the Error List when a row is double-clicked in order to open a file to be edited.
    /// </summary>
    /// <param name="sender">
    /// The instance raising the event.
    /// </param>
    /// <param name="e">
    /// An <see cref="ErrorEntryEventArgs"/> event arguments instance.
    /// </param>
    private void HandleOpenFileRequest(object? sender, ErrorEntryEventArgs e)
    {
        if (e.Entry is { FileName: { } fileName } entry && !string.IsNullOrWhiteSpace(fileName))
            OpenSource(fileName, entry.LineNumber, entry.ProjectName);
    }

    /// <summary>
    /// Shows the solution explorer pane.
    /// </summary>
    internal void ShowSolutionExplorer()
    {
        _toolWindows.Show(SolutionExplorerPane);
    }

    /// <summary>
    /// Shows the error list with all entries selected.
    /// </summary>
    internal void ShowErrorList()
    {
        ErrorList.SelectedFilter = "All";
        _toolWindows.Show(ErrorListPane);
    }

    /// <summary>
    /// Synchronizes the tool-window menu checks with pane visibility.
    /// </summary>
    /// <param name="sender">
    /// The pane that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void HandleToolWindowVisibilityChanged(object? sender, EventArgs e)
    {
        SolutionExplorerMenuItem.IsChecked = SolutionExplorerPane.IsVisible;
        ErrorListMenuItem.IsChecked = ErrorListPane.IsVisible;
        NamingAnalysisMenuItem.IsChecked = NamingAnalysisPane.IsVisible;
    }

    /// <summary>
    /// Applies the solution explorer menu's visibility selection.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void HandleSolutionExplorerVisibilityClick(object sender, RoutedEventArgs e)
    {
        _toolWindows.SetVisibility(SolutionExplorerPane, SolutionExplorerMenuItem.IsChecked);
    }

    /// <summary>
    /// Applies the error list menu's visibility selection.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void HandleErrorListVisibilityClick(object sender, RoutedEventArgs e)
    {
        _toolWindows.SetVisibility(ErrorListPane, ErrorListMenuItem.IsChecked);
    }

    /// <summary>
    /// Applies the naming analysis menu's visibility selection.
    /// </summary>
    /// <param name="sender">
    /// The menu item that raised the event.
    /// </param>
    /// <param name="e">
    /// The routed event data.
    /// </param>
    private void HandleNamingAnalysisVisibilityClick(object sender, RoutedEventArgs e)
    {
        _toolWindows.SetVisibility(NamingAnalysisPane, NamingAnalysisMenuItem.IsChecked);
    }

    /// <summary>
    /// Opens the source location for a naming observation or violation.
    /// </summary>
    /// <param name="sender">
    /// The results grid that raised the event.
    /// </param>
    /// <param name="e">
    /// The mouse event data.
    /// </param>
    private void HandleNamingRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid || e.OriginalSource is not DependencyObject source)
            return;
        DependencyObject? current = source;
        while (current is not null && current is not DataGridRow)
            current = VisualTreeHelper.GetParent(current);
        if (current is not DataGridRow row)
            return;

        NamingSourceSpan? location = row.Item switch
        {
            NamingObservation observation => observation.Location,
            NamingViolation violation => violation.Location,
            _ => null
        };
        string? projectName = row.Item switch
        {
            NamingObservation observation => observation.SourceFile?.Project?.Name,
            NamingViolation violation => violation.Project?.Name,
            _ => null
        };
        if (location is not null && !string.IsNullOrWhiteSpace(location.FilePath))
            OpenSource(location.FilePath, location.StartLine, projectName);
    }

    /// <summary>
    /// Opens the source file selected in the solution explorer.
    /// </summary>
    /// <param name="sender">
    /// The control that raised the event.
    /// </param>
    /// <param name="e">
    /// The selected solution explorer node.
    /// </param>
    private void HandleNodeDoubleClicked(object? sender, SolutionExplorerNodeEventArgs e)
    {
        string? include = e.Node?.File?.Include;
        string? projectPath = e.Node?.Project?.Project?.FilePath;
        if (string.IsNullOrWhiteSpace(include) || string.IsNullOrWhiteSpace(projectPath))
        {
            MessageBox.Show(this, "The file's project path is unavailable.", "Unable to open source", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        try
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(projectPath))!;
            OpenSource(Path.GetFullPath(include, directory), 0, e.Node?.Project?.Name);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            MessageBox.Show(this, exception.Message, "Unable to open source", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Opens a source editor and reports file-opening errors.
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
        if (_vm is null)
            return;
        try
        {
            _vm.OpenSource(fileName, line, projectName);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            MessageBox.Show(this, exception.Message, "Unable to open source", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Initializes the text-to-hex conversion view/page.
    /// </summary>
    private void InitializeTextToHexPage()
    {
        _textToHexPage = new TextToHexPage();
        TextToHexFrame.Content = _textToHexPage;
        TextToHexPane.Hide();
    }
    #endregion
}