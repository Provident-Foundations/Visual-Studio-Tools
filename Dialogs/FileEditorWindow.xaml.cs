using ICSharpCode.AvalonEdit;
using Provident.Foundations.Wpf.Controls;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace VS.Toolset.UI.Dialogs;

/// <summary>
/// Provides an editor for making code changes.
/// </summary>
public partial class FileEditorWindow : ProvidentWindowBase
{
    #region Private Member Declarations
    /// <summary>
    /// The view model.
    /// </summary>
    private FileEditorViewModel? _vm;
    #endregion

    #region Constructor / Dispose Methods
    /// <summary>
    /// Initializes a new instance of the <see cref="FileEditorWindow"/> class.
    /// </summary>
    /// <remarks>
    /// This is the default constructor.
    /// </remarks>
    public FileEditorWindow()
    {
        InitializeComponent();
        _vm = new FileEditorViewModel(this);

        TextEditor.Document = new ICSharpCode.AvalonEdit.Document.TextDocument();
        TextEditor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("XAML");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileEditorWindow"/> class.
    /// </summary>
    /// <param name="fileName">
    /// A string containing the path and name of the file to edit.
    /// </param>
    /// <param name="lineNumber">
    /// The line number to scrool to and highlight.
    /// </param>
    /// <param name="projectName">
    /// A string containing the name of the project that uses the file.
    /// </param>
    public FileEditorWindow(string fileName, int? lineNumber, string? projectName)
    {
        InitializeComponent();
        _vm = new FileEditorViewModel(this) { FileName = fileName, GotoLine = lineNumber };

        TextEditor.Document = new ICSharpCode.AvalonEdit.Document.TextDocument();
        TextEditor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(fileName));
        TextEditor.Load(fileName);
        Title = Path.GetFileName(fileName);
        TextEditorOptions opts = TextEditor.Options;
        opts.EnableHyperlinks = true;
        opts.EnableEmailHyperlinks = false;
        opts.ConvertTabsToSpaces = true;
        opts.IndentationSize = 4;
        opts.ShowSpaces = false;
        opts.ShowTabs = false;

    }
    #endregion

    #region Public Properties
    public string? FileName => _vm?.FileName;

    /// <summary>Navigates an existing editor to a one-based source line.</summary>
    public void NavigateToLine(int lineNumber)
    {
        if (_vm is not null && lineNumber > 0 && lineNumber <= TextEditor.Document.LineCount)
        {
            _vm.GotoLine = lineNumber;
            TextEditor.ScrollToLine(lineNumber);
            TextEditor.CaretOffset = TextEditor.Document.GetOffset(lineNumber, 1);
        }
    }
    #endregion

    private void HandleSaveClicked(object sender, RoutedEventArgs e) => SaveFile();

    private void HandleCloseClicked(object sender, RoutedEventArgs e) => Close();

    private bool SaveFile()
    {
        if (string.IsNullOrWhiteSpace(FileName))
        {
            MessageBox.Show(this, "No file is open.", "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        string? temporaryFile = null;
        try
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(FileName))!;
            temporaryFile = Path.Combine(directory, $".{Guid.NewGuid():N}.tmp");
            using (FileStream stream = new(temporaryFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                TextEditor.Save(stream);
            File.Replace(temporaryFile, FileName, null);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            TextEditor.IsModified = true;
            MessageBox.Show(this, exception.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        finally
        {
            if (temporaryFile is not null && File.Exists(temporaryFile))
            {
                try { File.Delete(temporaryFile); }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    MessageBox.Show(this, $"Could not remove temporary file '{temporaryFile}': {exception.Message}", "Cleanup failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }

    #region Protected Method Overrides
    protected override void OnClosing(CancelEventArgs e)
    {
        if (TextEditor.IsModified)
        {
            MessageBoxResult choice = MessageBox.Show(this, $"Save changes to {Title}?", "Unsaved changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            e.Cancel = choice == MessageBoxResult.Cancel || (choice == MessageBoxResult.Yes && !SaveFile());
        }
        base.OnClosing(e);
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        if (_vm?.GotoLine is int lineNumber)
        {
            NavigateToLine(lineNumber);
            _vm.GotoLine = null;
        }
    }
    #endregion
}
