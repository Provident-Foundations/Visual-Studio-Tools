using Provident.Foundations.VisualStudio.IO;
using Provident.Foundations.VisualStudio.Model;

namespace VS.Toolset.UI;

/// <summary>
/// Loads Visual Studio solutions and projects into workspace models.
/// </summary>
internal sealed class WorkspaceService
{
    private readonly VisualStudioSolutionLoader _solutionLoader = new();
    private readonly VisualStudioProjectLoader _projectLoader = new();

    /// <summary>
    /// Loads a solution from the specified path.
    /// </summary>
    /// <param name="fileName">
    /// The solution file path.
    /// </param>
    /// <returns>
    /// The loaded solution model.
    /// </returns>
    public VsSolutionModel LoadSolution(string fileName)
    {
        return _solutionLoader.LoadSolution(fileName);
    }

    /// <summary>
    /// Loads a project into a single-project workspace.
    /// </summary>
    /// <param name="fileName">
    /// The project file path.
    /// </param>
    /// <returns>
    /// A workspace containing the loaded project.
    /// </returns>
    public VsSolutionModel LoadProject(string fileName)
    {
        ProjectModel project = _projectLoader.Load(fileName);
        VsSolutionModel workspace = new() { Name = project.Name, FilePath = fileName };
        workspace.Projects.Add(new SolutionProjectModel { Name = project.Name, RelativePath = fileName, Project = project });
        return workspace;
    }
}
