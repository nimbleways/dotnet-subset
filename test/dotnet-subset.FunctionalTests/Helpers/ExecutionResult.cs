using System.Runtime.CompilerServices;
using System.Text;

using Nimbleways.Tools.Subset.Models;
using Nimbleways.Tools.Subset.Utils;

namespace Nimbleways.Tools.Subset.Helpers;

public class ExecutionResult
{
    public ExecutionResult(TestDescriptor testDescriptor, DirectoryInfo outputDirectory, int exitCode, string consoleOutput)
    {
        TestDescriptor = testDescriptor;
        OutputDirectory = outputDirectory;
        ExitCode = exitCode;
        ConsoleOutput = consoleOutput;
    }

    public TestDescriptor TestDescriptor { get; }
    public DirectoryInfo OutputDirectory { get; }
    public int ExitCode { get; }
    public string ConsoleOutput { get; }

    public SettingsTask VerifyOutput([CallerMemberName] string callerMethodName = "", [CallerFilePath] string callerFilePath = "")
    {
        string callerFileRelativeDirectoryPath = Path.GetDirectoryName(callerFilePath).AsNotNull();
        string rootVerifyDirectory = Path.GetFullPath(Path.Combine(TestHelpers.RepositoryRoot.FullName, callerFileRelativeDirectoryPath, "Verify"));
        string testClassVerifyDirectory = Path.Combine(rootVerifyDirectory, Path.GetFileNameWithoutExtension(callerFilePath));
        return Verify(ConsoleOutput, extension: "txt")
        .UseDirectory(testClassVerifyDirectory)
        .UseFileName($"ConsoleOutput_{TestDescriptor.OperationName}_{callerMethodName}_{TestDescriptor.TestName}")
            .AddScrubber(stringBuilder => stringBuilder.Replace('\\', '/'))
            .AddScrubber(stringBuilder => stringBuilder.Replace(OutputDirectory.FullName, "{OutputDirectory}"))
            .AddScrubber(stringBuilder => stringBuilder.Replace(TestDescriptor.Root.FullName, "{SourceRootDirectory}"))
            .AddScrubber(RemoveFatalErrorCallStack)
            .DisableDiff();
    }

    private void RemoveFatalErrorCallStack(StringBuilder stringBuilder)
    {
        string callStackPrefix = "   at ";
        string fullString = stringBuilder.ToString();
        int fatalErrorPosition = fullString.LastIndexOf("FATAL ERROR: ", StringComparison.Ordinal);
        if (fatalErrorPosition < 0)
        {
            return;
        }
        int stackPostion = fullString.IndexOf(callStackPrefix, fatalErrorPosition, StringComparison.Ordinal);
        stringBuilder.Length = stackPostion;
        stringBuilder.AppendLine(callStackPrefix + "{CallStack}");
    }
}
