using System.Diagnostics;

using Nimbleways.Tools.Subset.Models;

namespace Nimbleways.Tools.Subset.Helpers;

internal static class DotnetSubsetRunner
{
    public static string AssertRun(RestoreTestDescriptor restoreTestDescriptor, DirectoryInfo output)
    {
        ExecutionResult executionResult = Run(restoreTestDescriptor, output);
        Assert.Equal(restoreTestDescriptor.ExitCode, executionResult.ExitCode);
        return executionResult.ConsoleOutput;
    }
    public static string AssertRun(int overriddenExpectedExitCode, RestoreTestDescriptor restoreTestDescriptor, DirectoryInfo output)
    {
        ExecutionResult executionResult = Run(restoreTestDescriptor, output);
        Assert.Equal(overriddenExpectedExitCode, executionResult.ExitCode);
        return executionResult.ConsoleOutput;
    }

    private sealed record ExecutionResult(int ExitCode, string ConsoleOutput);

    private static ExecutionResult Run(RestoreTestDescriptor restoreTestDescriptor, DirectoryInfo output)
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

    private static ExecutionResult RunMain(string[] subsetArgs, DirectoryInfo workingDirectory)
    {
        lock (WorkingDirLock)
        {
            using StringWriter writer = new();
            (var originalOutTextWriter, var originalErrorTextWriter) = (Console.Out, Console.Error);
            Console.SetOut(writer);
            Console.SetError(writer);
            string previousCurrentDirectory = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = workingDirectory.FullName;
                return new(Program.Main(subsetArgs), writer.ToString());
            }
            finally
            {
                Environment.CurrentDirectory = previousCurrentDirectory;
                Console.SetOut(originalOutTextWriter);
                Console.SetError(originalErrorTextWriter);
            }
        }
    }

    private static ExecutionResult RunProcess(IEnumerable<string> subsetArgs, DirectoryInfo workingDirectory)
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

        return new(process.ExitCode, string.Empty);
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
