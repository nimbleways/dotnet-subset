using System.Runtime.CompilerServices;

using Nimbleways.Tools.Subset.Models;
using Nimbleways.Tools.Subset.Utils;

namespace Nimbleways.Tools.Subset.Helpers;

public class DescriptorExecutionResult : ExecutionResult
{
    public DescriptorExecutionResult(TestDescriptor testDescriptor, DirectoryInfo outputDirectory, int exitCode, string consoleOutput)
        : base(exitCode, consoleOutput)
    {
        TestDescriptor = testDescriptor;
        OutputDirectory = outputDirectory;
    }

    public TestDescriptor TestDescriptor { get; }
    public DirectoryInfo OutputDirectory { get; }

    public override SettingsTask VerifyOutput(Func<SettingsTask, SettingsTask>? configure = null, [CallerMemberName] string callerMethodName = "", [CallerFilePath] string callerFilePath = "")
    {
        string testClassVerifyDirectory = GetDirectory(callerFilePath);
        string fileName = $"ConsoleOutput_{TestDescriptor.OperationName}_{callerMethodName}_{TestDescriptor.TestName}";
        return Verify(configure, testClassVerifyDirectory, fileName)
            .AddScrubber(stringBuilder => stringBuilder.Replace(OutputDirectory.FullName, "{OutputDirectory}"))
            .AddScrubber(stringBuilder => stringBuilder.Replace(TestDescriptor.Root.FullName, "{SourceRootDirectory}"));
    }

    private static string GetDirectory(string callerFilePath)
    {
        string callerFileRelativeDirectoryPath = Path.GetDirectoryName(callerFilePath).AsNotNull();
        string rootVerifyDirectory = Path.GetFullPath(Path.Combine(TestHelpers.RepositoryRoot.FullName, callerFileRelativeDirectoryPath, "Verify"));
        return Path.Combine(rootVerifyDirectory, Path.GetFileNameWithoutExtension(callerFilePath));
    }
}
