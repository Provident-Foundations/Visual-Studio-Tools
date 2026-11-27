using Provident.Foundations.VisualStudio.Analysis.Naming;
using Provident.Foundations.VisualStudio.Model;
using System.IO;
using VS.Toolset.UI;

namespace VisualStudioToolset.Tests;

[TestClass]
public sealed class NamingAnalysisConfigurationTests
{
    [TestMethod]
    public async Task SettingsApplyToProjectAndSolutionAnalysis()
    {
        string file = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(file, "<Grid xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"><Button x:Name=\"TestButton\" /></Grid>");
            XamlFileModel xaml = new() { Include = file };
            xaml.Metadata.Add("Generated", "true");
            ProjectModel project = new() { Name = "Example", FilePath = Path.Combine(Path.GetDirectoryName(file)!, "Example.csproj") };
            project.XamlFiles.Add(xaml);
            project.ProjectReferences.Add(new ProjectReferenceModel { Include = "Dependency.csproj" });
            SolutionProjectModel entry = new() { Name = "Example", Project = project };
            VsSolutionModel solution = new() { Name = "Example" };
            solution.Projects.Add(entry);
            NamingAnalysisService service = new();
            IProgress<NamingAnalysisProgress> progress = new Progress<NamingAnalysisProgress>();

            using NamingSolutionAnalysisResult xamlDisabled = await service.AnalyzeAsync(solution, entry, new NamingAnalysisConfiguration { IncludeXaml = false, IncludeGeneratedFiles = true }, progress, CancellationToken.None);
            using NamingSolutionAnalysisResult generatedDisabled = await service.AnalyzeAsync(solution, entry, new NamingAnalysisConfiguration { IncludeXaml = true, IncludeGeneratedFiles = false }, progress, CancellationToken.None);
            using NamingSolutionAnalysisResult projectIncluded = await service.AnalyzeAsync(solution, entry, new NamingAnalysisConfiguration { IncludeXaml = true, IncludeGeneratedFiles = true }, progress, CancellationToken.None);
            using NamingSolutionAnalysisResult solutionIncluded = await service.AnalyzeAsync(solution, null, new NamingAnalysisConfiguration { IncludeXaml = true, IncludeGeneratedFiles = true }, progress, CancellationToken.None);

            Assert.AreEqual(0, xamlDisabled.Statistics.FileCount);
            Assert.AreEqual(1, generatedDisabled.Statistics.FileCount);
            Assert.AreEqual(0, generatedDisabled.Statistics.ParsedFileCount);
            Assert.AreEqual(1, projectIncluded.Statistics.ParsedFileCount);
            Assert.IsTrue(projectIncluded.Observations.Any(item => item.Name == "TestButton"));
            IReadOnlyList<string>? dependencies = projectIncluded.Projects[0].Dependencies;
            Assert.IsNotNull(dependencies);
            CollectionAssert.AreEqual(new[] { "Dependency.csproj" }, dependencies.ToArray());
            Assert.AreEqual(1, solutionIncluded.Statistics.ParsedFileCount);
            Assert.IsTrue(solutionIncluded.Observations.Any(item => item.Name == "TestButton"));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [TestMethod]
    public async Task UnloadedSelectedProjectDoesNotAnalyzeWholeSolution()
    {
        NamingAnalysisService service = new();
        InvalidOperationException exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.AnalyzeAsync(
            new VsSolutionModel(), new SolutionProjectModel { Name = "Unloaded" }, new NamingAnalysisConfiguration(),
            new Progress<NamingAnalysisProgress>(), CancellationToken.None));
        StringAssert.Contains(exception.Message, "no longer loaded");
    }
}
