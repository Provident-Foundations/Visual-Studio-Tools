using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Provident.Foundations.VisualStudio.Model;
using VS.Toolset.UI;
using VS.Toolset.UI.CodeReview;

internal static class Program
{
    static void Check(bool value, string label) { if (!value) throw new Exception(label); }
    static void Wait(Task task)
    {
        var frame = new DispatcherFrame();
        var dispatcher = Dispatcher.CurrentDispatcher;
        task.ContinueWith(_ => dispatcher.BeginInvoke(() => frame.Continue = false), TaskScheduler.Default);
        Dispatcher.PushFrame(frame);
        task.GetAwaiter().GetResult();
    }
    static void Git(string directory, params string[] args)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!;
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new Exception(output + error);
    }
    [STAThread]
    static void Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "ReviewNoRepoVerification-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string projectDir = Path.Combine(root, "project");
            string linkedDir = Path.Combine(root, "linked");
            Directory.CreateDirectory(projectDir);
            Directory.CreateDirectory(linkedDir);
            Directory.CreateDirectory(Path.Combine(projectDir, "obj"));
            string source = Path.Combine(projectDir, "Example.cs");
            string linked = Path.Combine(linkedDir, "Linked.cs");
            string sql = Path.Combine(projectDir, "migration.sql");
            File.WriteAllText(source, "class Example {\n const int BadName = 1;\n void M() { try { } catch { } }\n}\n");
            File.WriteAllText(linked, "class Linked { const int AlsoBad = 1; }\n");
            File.WriteAllText(sql, "IF EXISTS (SELECT 1) SELECT 1;\n");
            File.WriteAllText(Path.Combine(projectDir, "view.xaml"), "<Grid />\n");
            File.WriteAllText(Path.Combine(projectDir, "obj", "Generated.cs"), "class Generated { const int BadGenerated = 1; }\n");
            var project = new ProjectModel { FilePath = Path.Combine(projectDir, "Example.csproj") };
            project.CodeFiles.Add(new CodeFileModel { Include = "Example.cs" });
            project.CodeFiles.Add(new CodeFileModel { Include = "..\\linked\\Linked.cs" });
            project.CodeFiles.Add(new CodeFileModel { Include = "migration.sql" });
            project.CodeFiles.Add(new CodeFileModel { Include = "missing.cs" });
            project.CodeFiles.Add(new CodeFileModel { Include = "$(Missing)\\*.cs" });
            project.XamlFiles.Add(new XamlFileModel { Include = "view.xaml" });
            var workspace = new VsSolutionModel();
            workspace.Projects.Add(new SolutionProjectModel { Name = "Example", Project = project });
            workspace.Projects.Add(new SolutionProjectModel { Name = "Unavailable" });
            var analyzer = new CodeReviewAnalyzer();
            string? oldPath = Environment.GetEnvironmentVariable("PATH");
            Environment.SetEnvironmentVariable("PATH", "");
            ReviewReport report;
            try { report = analyzer.ReviewAllAsync(projectDir, workspace: workspace).GetAwaiter().GetResult(); }
            finally { Environment.SetEnvironmentVariable("PATH", oldPath); }
            Check(report.Repository is null && report.BaseCommit is null && report.Scope.Contains("No Git repository"), "non-Git scope");
            Check(report.Categories.Count == 11, "all categories");
            Check(report.Findings.Any(f => f.File == source && f.Line == 2 && f.Rule == "Constant naming"), "source location");
            Check(report.Findings.Any(f => f.File == linked && f.Rule == "Constant naming"), "linked source");
            Check(report.Findings.Any(f => f.File == sql && f.Rule == "Migration determinism"), "SQL check");
            Check(report.Findings.Any(f => f.Rule == "Scope" && f.Message.Contains("missing or unavailable")), "missing source warning");
            Check(report.Findings.Any(f => f.Rule == "Scope" && f.Message.Contains("could not be resolved")), "unresolved item warning");
            Check(report.Findings.Any(f => f.Rule == "Scope" && f.Message.Contains("Unavailable")), "unloaded project warning");
            Check(!report.Findings.Any(f => f.File?.Contains("Generated.cs") == true), "workspace scope excludes non-items");
            Check(JsonSerializer.Deserialize<ReviewReport>(JsonSerializer.Serialize(report))?.Repository is null, "JSON null repository");
            ReviewReport single = analyzer.ReviewCategoryAsync(projectDir, ReviewCategory.Naming).GetAwaiter().GetResult();
            Check(single.Categories.SequenceEqual(new[] { ReviewCategory.Naming }) && single.Findings.Any(f => f.File == source && f.Rule == "Constant naming"), "directory category");
            Check(!single.Findings.Any(f => f.File?.Contains("Generated.cs") == true), "directory excludes obj");
            using var cancel = new CancellationTokenSource(); cancel.Cancel();
            try { analyzer.ReviewAllAsync(projectDir, cancellationToken: cancel.Token).GetAwaiter().GetResult(); throw new Exception("cancellation ignored"); } catch (OperationCanceledException) { }
            string repo = Path.Combine(root, "repository"); Directory.CreateDirectory(repo);
            Git(repo, "init", "-b", "main");
            string repoFile = Path.Combine(repo, "Repo.cs");
            File.WriteAllText(repoFile, "class Repo { const int OldBad = 1; }\n");
            Git(repo, "add", ".");
            Git(repo, "-c", "user.name=Review Test", "-c", "user.email=test@example.invalid", "commit", "-m", "Test fixture\n\nCo-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>");
            File.AppendAllText(repoFile, "class Added { const int NewBad = 1; }\n");
            ReviewReport git = analyzer.ReviewCategoryAsync(repo, ReviewCategory.Naming).GetAwaiter().GetResult();
            Check(git.Repository is not null && git.BaseCommit is not null && git.Findings.Any(f => f.Rule == "Constant naming" && f.Line == 2) && !git.Findings.Any(f => f.Rule == "Constant naming" && f.Line == 1), "Git diff unchanged");
            string broken = Path.Combine(root, "broken"); Directory.CreateDirectory(broken); File.WriteAllText(Path.Combine(broken, ".git"), "invalid");
            try { analyzer.ReviewAllAsync(broken).GetAwaiter().GetResult(); throw new Exception("broken repository ignored"); } catch (InvalidOperationException) { }
            var app = new Application(); SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            var window = new MainWindow(); window.Show();
            var vm = (MainWindowViewModel)window.DataContext;
            typeof(MainWindowViewModel).GetField("_appPath", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(vm, projectDir);
            typeof(MainWindowViewModel).GetProperty("Solution")!.SetValue(vm, workspace);
            var command = (CommunityToolkit.Mvvm.Input.IAsyncRelayCommand<string>)vm.ReviewCommand;
            Wait(command.ExecuteAsync("All"));
            Check(!vm.IsBusy && vm.Errors.Any(e => e.FileName == source && e.LineNumber == 2 && e.Description!.Contains("Constant naming")), "UI results");
            Check(vm.ExportReviewCommand.CanExecute(null), "UI export enabled");
            window.Close(); app.Shutdown();
            Console.WriteLine("Non-repository and Git review verification passed.");
        }
        finally
        {
            foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(root, true);
        }
    }
}
