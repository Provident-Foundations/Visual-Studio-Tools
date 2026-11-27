using System.Windows.Controls;
using Visual.Studio.Toolset.UI.Menu;

namespace VS.Toolset.UI.Controls.Menu;

/// <summary>
/// Provides the UI definition and basic logic for the File menu for the main application.
/// </summary>
public partial class FileMenuControl : MenuItem, IDisposable
{
    #region Constructor / Dispose Methods
    /// <summary>
    /// Initializes a new instance of the <see cref="FileMenuControl"/> class.
    /// </summary>
    public FileMenuControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Releases all resources used by the <see cref="FileMenuControl"/> instance.
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
    /// The <see cref="IMainMenuViewModel"/> implementation instance containing the commands for the menu items.
    /// </param>
    public void SetContext(IMainMenuViewModel dataContext)
    {
        DataContext = dataContext;
    }
}