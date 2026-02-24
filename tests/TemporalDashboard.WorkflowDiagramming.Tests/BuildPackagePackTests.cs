using System.Diagnostics;
using System.IO.Compression;

namespace TemporalDashboard.WorkflowDiagramming.Tests;

/// <summary>
/// Tests that the WorkflowDiagramming.Build NuGet package is packed with all required files
/// (task DLL plus dependency DLLs in lib/) so consumers do not get FileNotFoundException.
/// </summary>
public class BuildPackagePackTests
{
    private static string GetBuildProjectPath()
    {
        var testDir = Path.GetDirectoryName(typeof(BuildPackagePackTests).Assembly.Location)
            ?? throw new InvalidOperationException("Could not get test assembly directory.");
        // From .../tests/.../bin/Debug/net10.0 go up to repo root then into Build project
        // bin/Debug/net10.0 -> 5 levels up = repo root
        var repoRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", ".."));
        var buildProj = Path.Combine(repoRoot, "src", "TemporalDashboard.WorkflowDiagramming.Build",
            "TemporalDashboard.WorkflowDiagramming.Build.csproj");
        if (!File.Exists(buildProj))
            throw new InvalidOperationException($"Build project not found at: {buildProj}");
        return buildProj;
    }

    [Fact]
    public void Pack_BuildProject_IncludesTaskAndDependencyDllsInLib()
    {
        var buildProj = GetBuildProjectPath();
        var outDir = Path.Combine(Path.GetTempPath(), "TemporalDashboard.Build.PackTests", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(outDir);

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList = { "pack", buildProj, "-c", "Release", "-o", outDir, "-p:Version=1.0.0.0-test" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start dotnet pack.");
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(TimeSpan.FromSeconds(120));

            Assert.True(process.ExitCode == 0,
                $"dotnet pack failed (exit {process.ExitCode}). stdout: {stdout}. stderr: {stderr}.");

            var nupkgs = Directory.GetFiles(outDir, "*.nupkg");
            Assert.Single(nupkgs);
            var nupkgPath = nupkgs[0];

            using var zip = ZipFile.OpenRead(nupkgPath);
            var entries = zip.Entries.Select(e => e.FullName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Task DLL (always included by SDK)
            Assert.Contains(entries, e => e.EndsWith("TemporalDashboard.WorkflowDiagramming.Build.dll", StringComparison.OrdinalIgnoreCase));

            // Dependency DLLs required so the task can load them from the package (fix for FileNotFoundException)
            Assert.True(entries.Any(e => e.Contains("TemporalDashboard.WorkflowDiagramming.dll", StringComparison.OrdinalIgnoreCase)),
                "Package must include TemporalDashboard.WorkflowDiagramming.dll in lib so the task can load it.");
            Assert.True(entries.Any(e => e.Contains("Temporalio.dll", StringComparison.OrdinalIgnoreCase)),
                "Package must include Temporalio.dll in lib so the task can load it.");

            // Build targets
            Assert.True(entries.Any(e => e.Contains(".targets", StringComparison.OrdinalIgnoreCase)),
                "Package must include the .targets file.");
        }
        finally
        {
            if (Directory.Exists(outDir))
            {
                try { Directory.Delete(outDir, true); } catch { /* ignore */ }
            }
        }
    }
}
