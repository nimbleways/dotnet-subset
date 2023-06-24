using System.Diagnostics;

using Nimbleways.Tools.Subset.Models;

namespace Nimbleways.Tools.Subset.Helpers;

internal static class DotnetSubsetRunner
{
    public static void AssertRun(RestoreTestDescriptor restoreTestDescriptor, DirectoryInfo output)
    {
        Assert.Equal(restoreTestDescriptor.ExitCode, Run(restoreTestDescriptor, output));
    }
    public static void AssertRun(int overriddenExpectedExitCode, RestoreTestDescriptor restoreTestDescriptor, DirectoryInfo output)
    {
        Assert.Equal(overriddenExpectedExitCode, Run(restoreTestDescriptor, output));
    }

    private static int Run(RestoreTestDescriptor restoreTestDescriptor, DirectoryInfo output)
    {
        string[] subsetArgs = GetSubsetArgs(restoreTestDescriptor, output);
        DirectoryInfo workingDirectory = restoreTestDescriptor.Root;
        return IsRunningInCI()
            ? RunProcess(subsetArgs, workingDirectory)
            : RunMain(subsetArgs, workingDirectory);
    }

    private static bool IsRunningInCI()
    {
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CI"));
    }

    private static readonly object WorkingDirLock = new();

    private static int RunMain(string[] subsetArgs, DirectoryInfo workingDirectory)
    {
        lock (WorkingDirLock)
        {
            string previousCurrentDirectory = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = workingDirectory.FullName;
                return Program.Main(subsetArgs);
            }
            finally
            {
                Environment.CurrentDirectory = previousCurrentDirectory;
            }
        }
    }

    private static int RunProcess(IEnumerable<string> subsetArgs, DirectoryInfo workingDirectory)
    {
        string subsetArgsString = string.Join(" ", subsetArgs.Select(a => $@"""{a}"""));
        using Process process = new();

        // Configure the process
        process.StartInfo.FileName = "dotnet";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.RedirectStandardError = false;
        process.StartInfo.CreateNoWindow = false;
        process.StartInfo.WorkingDirectory = workingDirectory.FullName;
        process.StartInfo.Arguments = $@"subset {subsetArgsString}";

        // Start the process
        process.Start();

        // Wait for the process to exit
        process.WaitForExit();

        return process.ExitCode;
    }

    private static string[] GetSubsetArgs(RestoreTestDescriptor restoreTestDescriptor, DirectoryInfo output)
    {
        string projectOrSolution = Path.Combine(restoreTestDescriptor.Root.FullName, restoreTestDescriptor.CommandInputs.ProjectOrSolution);
        return new[]
        {
            "restore",
            projectOrSolution,
            "--output",
            output.FullName,
            "--root-directory",
            restoreTestDescriptor.Root.FullName
        };
    }
}
