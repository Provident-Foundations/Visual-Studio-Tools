using Provident.Foundations.Wpf.Controls.Mvvm;
using System.IO;
using System.Windows;

namespace VS.Toolset.UI.Dialogs;

/// <summary>
/// Provides the view model implementation for the filed editing dialog.
/// </summary>
public sealed partial class FileEditorViewModel : ViewModelBase
{
    #region Private Member Declarations
    /// <summary>
    /// The path and name of the file being edited.
    /// </summary>
    private string? _fileName;
    /// <summary>
    /// The document instance.
    /// </summary>
    private ICSharpCode.AvalonEdit.Document.TextDocument? _document;
    /// <summary>
    /// The actual code text.
    /// </summary>
    private string? _fileText;

    /// <summary>
    /// The line number to go to.
    /// </summary>
    private int? _gotoLine;
    #endregion

    #region Constructor / Dispose Methods
    /// <summary>
    /// Initializes a new instance of the <see cref="FileEditorViewModel"/> class.
    /// </summary>
    /// <remarks>
    /// This is the default constructor.
    /// </remarks>
    public FileEditorViewModel()
    {
        _fileName = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileEditorViewModel"/> class.
    /// </summary>
    /// <param name="fileName">
    /// A string containing the path and name of the file being edited.
    /// </param>
    public FileEditorViewModel(string fileName)
    {
        _fileName = fileName;
        ReadFile();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileEditorViewModel"/> class.
    /// </summary>
    /// <param name="parent">
    /// The reference to the parent window.
    /// </param>
    public FileEditorViewModel(FrameworkElement parent) : base(parent)
    {
        parent.DataContext = this;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="FileEditorViewModel"/> class.
    /// </summary>
    /// <param name="parent">
    /// The reference to the parent window.
    /// </param>
    /// <param name="fileName">
    /// A string containing the path and name of the file being edited.
    /// </param>
    public FileEditorViewModel(FrameworkElement parent, string fileName) : base(parent)
    {
        parent.DataContext = this;
        _fileName = fileName;
        ReadFile();
    }

    /// <summary>
    /// Cleans up any resources being used.
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
        _fileName = null;
        _document = null;
        _fileText = null;
        base.Dispose(disposing);
    }
    #endregion

    #region Public Properties
    /// <summary>
    /// Gets or sets the reference to the text document being edited.
    /// </summary>
    /// <value>
    /// A <see cref="ICSharpCode.AvalonEdit.Document.TextDocument"/> instance.
    /// </value>
    public ICSharpCode.AvalonEdit.Document.TextDocument? Document
    {
        get => _document;
        set
        {
            SetProperty(ref _document, value);
        }
    }

    /// <summary>
    /// Gets or sets the path and name of the file being edited.
    /// </summary>
    /// <value>
    /// A string containing the path and name of the file.
    /// </value>
    public string? FileName
    {
        get => _fileName;
        set
        {
            SetProperty(ref _fileName, value);
        }
    }

    /// <summary>
    /// Gets or sets the text containing the code content.
    /// </summary>
    /// <value>
    /// A string containing the code content, or <b>null</b>.
    /// </value>
    public string? FileText
    {
        get => _fileText;
        set
        {
            SetProperty<string?>(ref _fileText, value);
        }
    }

    /// <summary>
    /// Gets or sets the line number to go to when displayed.
    /// </summary>
    /// <value>
    /// An integer representing the line number to go to, or <b>null</b> if not specified.
    /// </value>
    public int? GotoLine
    {
        get => _gotoLine;
        set
        {
            SetProperty(ref _gotoLine, value);
        }
    }
    #endregion

    #region Private Member Declarations
    /// <summary>
    /// Reads the content of the file.
    /// </summary>
    private void ReadFile()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(_fileName);
        FileText = File.ReadAllText(_fileName);
        _document = new ICSharpCode.AvalonEdit.Document.TextDocument(FileText);
    }
    #endregion
}
