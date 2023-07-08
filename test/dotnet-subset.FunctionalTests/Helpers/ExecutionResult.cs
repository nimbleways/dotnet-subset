using System.CommandLine;
using System.Runtime.CompilerServices;
using System.Text;

using Nimbleways.Tools.Subset.Utils;

namespace Nimbleways.Tools.Subset.Helpers;

public class ExecutionResult
{
    private readonly string _applicationName;

    public int ExitCode { get; }
    public string ConsoleOutput { get; }
    public bool IsOutOfProcess { get; }

    public ExecutionResult(int exitCode, string consoleOutput, bool isOutOfProcess)
    {
        ExitCode = exitCode;
        ConsoleOutput = consoleOutput;
        IsOutOfProcess = isOutOfProcess;
        _applicationName = isOutOfProcess ? "dotnet-subset" : RootCommand.ExecutableName;
    }

    public virtual SettingsTask VerifyOutput(Func<SettingsTask, SettingsTask>? configure = null, [CallerMemberName] string callerMethodName = "", [CallerFilePath] string callerFilePath = "")
    {
        string testClassVerifyDirectory = GetDirectory(callerFilePath);
        string fileName = $"ConsoleOutput_{callerMethodName}";
        return Verify(configure, testClassVerifyDirectory, fileName);
    }

    protected SettingsTask Verify(Func<SettingsTask, SettingsTask>? configure, string directory, string fileName)
    {
        SettingsTask verifyTask = Verifier
            .Verify(ConsoleOutput, extension: "txt")
            .UseDirectory(directory)
            .UseFileName(fileName);
        if (configure is not null)
        {
            verifyTask = configure(verifyTask);
        }
        return verifyTask
        .AddScrubber(stringBuilder => stringBuilder.Replace('\\', '/'))
        .AddScrubber(RemoveFatalErrorCallStack)
        .AddScrubber(stringBuilder => stringBuilder.Replace(_applicationName, "{ApplicationName}"))
        .DisableDiff();
    }

    private static string GetDirectory(string callerFilePath)
    {
        string callerFileRelativeDirectoryPath = Path.GetDirectoryName(callerFilePath).AsNotNull();
        string rootVerifyDirectory = Path.GetFullPath(Path.Combine(TestHelpers.RepositoryRoot.FullName, callerFileRelativeDirectoryPath, "Verify"));
        return Path.Combine(rootVerifyDirectory, Path.GetFileNameWithoutExtension(callerFilePath));
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
