using Nimbleways.Tools.Subset.Models;
using Nimbleways.Tools.Subset.Utils.Processes;

namespace Nimbleways.Tools.Subset.Helpers;

internal static class DotnetSubsetRunner
{
    public static ExecutionResult AssertRun(RestoreTestDescriptor restoreTestDescriptor, DirectoryInfo output, int? overriddenExpectedExitCode = null, bool noLogo = true)
    {
        ExecutionResult executionResult = Run(restoreTestDescriptor, output, noLogo);
        Assert.Equal(overriddenExpectedExitCode ?? restoreTestDescriptor.ExitCode, executionResult.ExitCode);
        return executionResult;
    }

    private static ExecutionResult Run(RestoreTestDescriptor restoreTestDescriptor, DirectoryInfo output, bool noLogo)
    {
        var subsetArgs = GetSubsetArgs(restoreTestDescriptor, output, noLogo);
        DirectoryInfo workingDirectory = restoreTestDescriptor.Root;
        InternalResult result = IsRunningInCI()
            ? RunProcess(subsetArgs, workingDirectory)
            : RunMain(subsetArgs, workingDirectory);
        return new ExecutionResult(restoreTestDescriptor, output, result.ExitCode, result.ConsoleOutput);
    }

    private static bool IsRunningInCI()
    {
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CI"));
    }

    private sealed record InternalResult(int ExitCode, string ConsoleOutput);


    private static readonly object WorkingDirLock = new();

    private static InternalResult RunMain(string[] subsetArgs, DirectoryInfo workingDirectory)
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

    private static InternalResult RunProcess(IEnumerable<string> subsetArgs, DirectoryInfo workingDirectory)
    {
        string subsetArgsString = string.Join(" ", subsetArgs.Select(a => $@"""{a}"""));

        using ProcessRunner processRunner = new(workingDirectory, "dotnet", $"subset {subsetArgsString}");

        ProcessResult processResult = processRunner.Run(TimeSpan.FromSeconds(30));

        return processResult switch
        {
            ProcessExitedResult result => new(result.ExitCode, result.Output),
            ProcessFailureResult { Exception: var exception } => throw exception,
            _ => throw new NotSupportedException()
        };
    }

    private static string[] GetSubsetArgs(RestoreTestDescriptor restoreTestDescriptor, DirectoryInfo output, bool noLogo)
    {
        string projectOrSolution = Path.Combine(restoreTestDescriptor.Root.FullName, restoreTestDescriptor.CommandInputs.ProjectOrSolution);
        var args = new[]
        {
            "restore",
            projectOrSolution,
            "--output",
            output.FullName,
            "--root-directory",
            restoreTestDescriptor.Root.FullName
        };
        if (noLogo)
        {
            args = args.Append("--nologo").ToArray();
        }
        return args;
    }
}
