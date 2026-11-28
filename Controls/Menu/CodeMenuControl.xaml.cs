using System.Windows.Controls;

namespace VS.Toolset.UI.Controls.Menu;

/// <summary>
/// Provides the UI definition and basic logic for the Code menu for the main application.
/// </summary>
public partial class CodeMenuControl : MenuItem, IDisposable
{
    #region Constructor / Dispose Methods
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeMenuControl"/> class.
    /// </summary>
    public CodeMenuControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Releases all resources used by the <see cref="CodeMenuControl"/> instance.
    /// </summary>
    public void Dispose()
    {
        DataContext = null;
        GC.SuppressFinalize(this);
    }
    #endregion

    /// <summary>
    /// Sets the refrence to the view model used in the main dialog.
    /// </summary>
    /// <param name="dataContext">
    /// The <see cref="MainWindowViewModel"/> instance containing the commands for the menu items.
    /// </param>
    protected void SetContext(MainWindowViewModel dataContext)
    {
        DataContext = dataContext;
    }
}