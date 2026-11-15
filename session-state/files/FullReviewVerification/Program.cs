using System.Diagnostics;
using Provident.Foundations.VisualStudio.Model;
using VS.Toolset.UI.CodeReview;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
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
string root = Path.Combine(Path.GetTempPath(), "FullReviewVerification-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    string repo = Path.Combine(root, "repo");
    string linkedDir = Path.Combine(root, "linked");
    Directory.CreateDirectory(repo);
    Directory.CreateDirectory(linkedDir);
    Git(repo, "init", "-b", "main");
    string clean = Path.Combine(repo, "Clean.cs");
    string modified = Path.Combine(repo, "Modified.cs");
    string staged = Path.Combine(repo, "Staged.cs");
    string untracked = Path.Combine(repo, "Untracked.cs");
    string ignored = Path.Combine(repo, "Ignored.cs");
    string linked = Path.Combine(linkedDir, "Linked.cs");
    File.WriteAllText(clean, "class Clean { const int BadClean = 1; }\n");
    File.WriteAllText(modified, "class Modified { const int BadOld = 1; }\n");
    File.WriteAllText(staged, "class Staged { const int BadStage = 1; }\n");
    File.WriteAllText(Path.Combine(repo, ".gitignore"), "Ignored.cs\n");
    Git(repo, "add", ".");
    Git(repo, "-c", "user.name=Test", "-c", "user.email=test@example.invalid", "commit", "-m", "fixture");
    var analyzer = new CodeReviewAnalyzer();
    var cleanReport = await analyzer.ReviewCategoryAsync(repo, ReviewCategory.Naming);
    Check(cleanReport.Findings.Any(f => f.File == clean && f.Rule == "Constant naming"), "clean repository scan");
    File.AppendAllText(modified, "class Added { const int BadNew = 1; }\n");
    File.AppendAllText(staged, "class StagedAddition { const int BadStagedAddition = 1; }\n");
    Git(repo, "add", "Staged.cs");
    File.WriteAllText(untracked, "class Untracked { const int BadUntracked = 1; }\n");
    File.WriteAllText(ignored, "class Ignored { const int BadIgnored = 1; }\n");
    File.WriteAllText(linked, "class Linked { const int BadLinked = 1; }\n");
    string sql = Path.Combine(repo, "Migration.sql");
    File.WriteAllText(sql, "IF EXISTS (SELECT 1) SELECT 1;\n");
    string sqlDirectory = Path.Combine(repo, "MiGrAtIoNs");
    Directory.CreateDirectory(sqlDirectory);
    string directoryMigration = Path.Combine(sqlDirectory, "001_create.SQL");
    File.WriteAllText(directoryMigration, "IF EXISTS (SELECT 1) SELECT 1;\n");
    string unrelatedSql = Path.Combine(repo, "query.sql");
    File.WriteAllText(unrelatedSql, "IF EXISTS (SELECT 1) SELECT 1;\n");
    string linkedMigrationDir = Path.Combine(linkedDir, "migrate");
    Directory.CreateDirectory(linkedMigrationDir);
    string linkedMigration = Path.Combine(linkedMigrationDir, "02.sql");
    File.WriteAllText(linkedMigration, "IF EXISTS (SELECT 1) SELECT 1;\n");
    string vb = Path.Combine(repo, "Example.VB");
    File.WriteAllText(vb, "Class Example\nEnd Class\n");
    string xaml = Path.Combine(repo, "view.xaml");
    File.WriteAllText(xaml, "<Grid />");
    string binary = Path.Combine(repo, "asset.bin");
    File.WriteAllBytes(binary, [0, 255, 0]);
    Directory.CreateDirectory(Path.Combine(repo, "obj"));
    File.WriteAllText(Path.Combine(repo, "obj", "Generated.cs"), "class Generated { const int BadGenerated = 1; }");
    var project = new ProjectModel { FilePath = Path.Combine(repo, "project.csproj") };
    project.CodeFiles.Add(new CodeFileModel { Include = "..\\linked\\Linked.cs" });
    project.CodeFiles.Add(new CodeFileModel { Include = "missing.cs" });
    project.CodeFiles.Add(new CodeFileModel { Include = "query.sql" });
    project.CodeFiles.Add(new CodeFileModel { Include = "..\\linked\\migrate\\02.sql" });
    project.XamlFiles.Add(new XamlFileModel { Include = "view.xaml" });
    var workspace = new VsSolutionModel();
    workspace.Projects.Add(new SolutionProjectModel { Name = "Project", Project = project });
    string? oldPath = Environment.GetEnvironmentVariable("PATH");
    ReviewReport report;
    try { Environment.SetEnvironmentVariable("PATH", ""); report = await analyzer.ReviewAllAsync(repo, workspace: workspace); }
    finally { Environment.SetEnvironmentVariable("PATH", oldPath); }
    foreach (string file in new[] { clean, modified, staged, untracked, ignored, linked })
        Check(report.Findings.Any(f => f.File == file && f.Rule == "Constant naming"), $"Missing finding for {file}");
    Check(report.Findings.Any(f => f.File == modified && f.Line == 1 && f.Rule == "Constant naming") && report.Findings.Any(f => f.File == modified && f.Line == 2 && f.Rule == "Constant naming"), "Both unchanged and changed lines");
    Check(report.Findings.Any(f => f.File == sql && f.Rule == "Migration determinism"), "named migration SQL check");
    Check(report.Findings.Any(f => f.File == directoryMigration && f.Rule == "Migration determinism"), "migration directory SQL check");
    Check(report.Findings.Any(f => f.File == linkedMigration && f.Rule == "Migration determinism"), "linked migration SQL check");
    Check(report.Findings.Where(f => f.File == sql || f.File == directoryMigration || f.File == linkedMigration).All(f => f.Category == ReviewCategory.Database), "SQL only in database review");
    Check(report.Findings.Any(f => f.File == vb && f.IsManual && f.Message.Contains("Visual Basic")), "VB manual coverage");
    Check(!report.Findings.Any(f => f.File == binary || f.File == xaml || f.File == unrelatedSql), "exclude non-reviewable files");
    Check(report.Findings.Any(f => f.Rule == "Scope" && f.Message.Contains("missing or unavailable")), "missing linked item");
    Check(report.Findings.Any(f => f.File?.EndsWith("Generated.cs") == true && f.Rule == "Constant naming"), "include build output");
    Check(!report.Findings.Any(f => f.File?.Contains("\\.git\\") == true), "exclude Git metadata");
    Check(report.Repository == repo && report.BaseCommit is null && report.Scope.Contains("regardless of Git status"), "scope metadata");
    Check(report.Categories.Count == 11, "all categories");
    string nested = Path.Combine(repo, "nested");
    Directory.CreateDirectory(nested);
    var nestedReport = await analyzer.ReviewCategoryAsync(nested, ReviewCategory.Naming);
    Check(nestedReport.Repository == repo && nestedReport.Findings.Any(f => f.File == clean && f.Rule == "Constant naming"), "nested solution scans containing repository");
    string later = Path.Combine(repo, "Later.cs");
    File.WriteAllText(later, "class Later { const int BadLater = 1; }\n");
    var single = await analyzer.ReviewCategoryAsync(repo, ReviewCategory.Naming);
    Check(single.Findings.Any(f => f.File == untracked && f.Rule == "Constant naming") && single.Categories.SequenceEqual([ReviewCategory.Naming]), "single-category directory scan");
    Check(single.Findings.Any(f => f.File == later && f.Rule == "Constant naming"), "rescan new files");
    using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
    try { await analyzer.ReviewAllAsync(repo, cancellationToken: cancellation.Token); throw new Exception("Cancellation ignored"); } catch (OperationCanceledException) { }
    Console.WriteLine("Full review verification passed.");
}
finally
{
    foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
    Directory.Delete(root, true);
}
