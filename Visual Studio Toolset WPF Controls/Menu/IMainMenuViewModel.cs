using CommunityToolkit.Mvvm.Input;

namespace Visual.Studio.Toolset.UI.Menu;

/// <summary>
/// Provides the signature definition for the view model that works with the menu controls.
/// </summary>
public interface IMainMenuViewModel
{
    #region File Menu Commands

    #region Visual Studio Solutions And Projects
    /// <summary>
    /// Gets or sets the reference to the command used to close the currently open document.
    /// </summary>
    public IRelayCommand CloseOpenDocumentCommand { get; }

    /// <summary>
    /// Gets or sets the reference to the command used to close an open Visual Studio project.
    /// </summary>
    public IRelayCommand CloseProjectCommand { get; }

    /// <summary>
    /// Gets or sets the reference to the command used to open a Visual Studio project.
    /// </summary>
    public IRelayCommand OpenProjectCommand { get; }

    /// <summary>
    /// Gets or sets the reference to the command used to close an open Visual Studio solution.
    /// </summary>
    public IRelayCommand CloseSolutionCommand { get; }

    /// <summary>
    /// Gets or sets the reference to the command used to open a Visual Studio solution.
    /// </summary>
    public IRelayCommand OpenSolutionCommand { get; }
    #endregion

    #region SQL Maker Projects

    /// <summary>
    /// Gets or sets the reference to the command used to create a new SQL Maker project.
    /// </summary>
    public IRelayCommand CreateNewSqlMakerProjectCommand { get; }

    /// <summary>
    /// Gets or sets the reference to the command used to close a SQL Maker project.
    /// </summary>
    public IRelayCommand CloseSqlMakerProjectCommand { get; }

    /// <summary>
    /// Gets or sets the reference to the command used to open a SQL Maker project.
    /// </summary>
    public IRelayCommand OpenSqlMakerProjectCommand { get; }
    #endregion

    #region General
    /// <summary>
    /// Gets or sets the reference to the command used to indicate the user is exiting the application.
    /// </summary>
    public IRelayCommand ExitApplicationCommand { get; }

    /// <summary>
    /// Gets or sets the reference to the command used to save the current document.
    /// </summary>
    public IRelayCommand SaveCommand { get; }

    /// <summary>
    /// Gets or sets the reference to the command used to save all open documents.
    /// </summary>
    public IRelayCommand SaveAllCommand { get; }

    #endregion
    
    #endregion

    #region Tools Menu

    #endregion

    #region File Menu
    #endregion

    #region File Menu
    #endregion

    #region File Menu
    #endregion

    #region File Menu
    #endregion

}
