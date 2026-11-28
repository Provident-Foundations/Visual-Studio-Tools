using System.Windows.Controls;

namespace VS.Toolset.UI.Controls.Menu;

/// <summary>
/// Provides the UI definition and basic logic for the Tool menu for the main application.
/// </summary>
public partial class ToolsMenuControl : MenuItem, IDisposable
{
    #region Constructor / Dispose Methods
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolsMenuControl"/> class.
    /// </summary>
    public ToolsMenuControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Releases all resources used by the <see cref="ToolsMenuControl"/> instance.
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
    public void SetContext(MainWindowViewModel dataContext)
    {
        DataContext = dataContext;
    }
}