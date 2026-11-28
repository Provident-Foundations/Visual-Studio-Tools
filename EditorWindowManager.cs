using Provident.Foundations.Abstractions;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using VS.Toolset.UI.Dialogs;

namespace VS.Toolset.UI;

/// <summary>
/// Tracks editor windows and opens or activates an editor for a source file.
/// </summary>
internal sealed class EditorWindowManager : DisposableObjectBase
{
    private readonly Dictionary<string, FileEditorWindow> _windows = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the collection of open editor windows.
    /// </summary>
    public ObservableCollection<FileEditorWindow> Windows { get; } = [];

    /// <summary>
    /// Opens a source file or activates its existing editor.
    /// </summary>
    /// <param name="owner">
    /// The owner of a newly created editor window.
    /// </param>
    /// <param name="fileName">
    /// The source file path.
    /// </param>
    /// <param name="line">
    /// The line to navigate to, if supplied.
    /// </param>
    /// <param name="projectName">
    /// The name of the associated project, if available.
    /// </param>
    public void Open(Window owner, string fileName, int? line, string? projectName)
    {
        string path = Path.GetFullPath(fileName);
        if (_windows.TryGetValue(path, out FileEditorWindow? existing))
        {
            if (line is int lineNumber)
                existing.NavigateToLine(lineNumber);
            existing.Activate();
            return;
        }

        FileEditorWindow editor = new(path, line, projectName)
        {
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            WindowState = WindowState.Maximized,
            WindowStyle = WindowStyle.SingleBorderWindow
        };
        editor.Show();
        Register(path, editor);
    }

    /// <summary>
    /// Determines whether an editor is registered for a file.
    /// </summary>
    /// <param name="fileName">
    /// The registered file path.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if an editor is registered; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Contains(string fileName) => _windows.ContainsKey(fileName);

    /// <summary>
    /// Gets the editor registered for a file.
    /// </summary>
    /// <param name="fileName">
    /// The registered file path.
    /// </param>
    /// <returns>
    /// The registered editor window.
    /// </returns>
    public FileEditorWindow Get(string fileName) => _windows[fileName];

    /// <summary>
    /// Registers an editor window if the file is not already registered.
    /// </summary>
    /// <param name="fileName">
    /// The file path used to identify the window.
    /// </param>
    /// <param name="window">
    /// The editor window to register.
    /// </param>
    public void Register(string fileName, FileEditorWindow window)
    {
        if (_windows.TryAdd(fileName, window))
        {
            Windows.Add(window);
            window.Closed += HandleWindowClosed;
        }
    }

    /// <summary>
    /// Requests that all editors close, stopping if a window remains open.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if all editors closed; otherwise, <see langword="false"/>.
    /// </returns>
    public bool CloseAll()
    {
        foreach (FileEditorWindow window in Windows.ToList())
        {
            window.Close();
            if (Windows.Contains(window))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Removes a closed editor from the tracked windows.
    /// </summary>
    /// <param name="sender">
    /// The editor that raised the event.
    /// </param>
    /// <param name="e">
    /// The event data.
    /// </param>
    private void HandleWindowClosed(object? sender, EventArgs e)
    {
        if (sender is FileEditorWindow window)
        {
            window.Closed -= HandleWindowClosed;
            Windows.Remove(window);
            if (window.FileName is string fileName)
                _windows.Remove(fileName);
        }
    }

    /// <summary>
    /// Releases the manager's event subscriptions and references to editor windows.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release managed resources; otherwise, <see langword="false"/>.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (!IsDisposed && disposing)
        {
            foreach (FileEditorWindow window in Windows)
                window.Closed -= HandleWindowClosed;
            Windows.Clear();
            _windows.Clear();
        }
        base.Dispose(disposing);
    }
}
