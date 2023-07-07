using System.Runtime.CompilerServices;
using System.Text;

using Nimbleways.Tools.Subset.Utils;

namespace Nimbleways.Tools.Subset.Helpers;

public class ExecutionResult
{
    public int ExitCode { get; }
    public string ConsoleOutput { get; }

    public ExecutionResult(int exitCode, string consoleOutput)
    {
        ExitCode = exitCode;
        ConsoleOutput = consoleOutput;
    }

    public virtual SettingsTask VerifyOutput([CallerMemberName] string callerMethodName = "", [CallerFilePath] string callerFilePath = "")
    {
        string testClassVerifyDirectory = GetDirectory(callerFilePath);
        string fileName = $"ConsoleOutput_{callerMethodName}";
        return Verify(testClassVerifyDirectory, fileName);
    }

    protected SettingsTask Verify(string directory, string fileName)
    {
        return Verifier.Verify(ConsoleOutput, extension: "txt")
        .UseDirectory(directory)
        .UseFileName(fileName)
        .AddScrubber(stringBuilder => stringBuilder.Replace('\\', '/'))
        .AddScrubber(RemoveFatalErrorCallStack)
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
