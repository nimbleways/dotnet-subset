namespace Nimbleways.Tools.Subset.Models;

public abstract record TestDescriptor(DirectoryInfo SampleDirectory, string RootRelativePath, string OperationName, string TestName, int ExitCode)
{
    public DirectoryInfo Root { get; } = GetRoot(SampleDirectory, RootRelativePath);
    public DirectoryInfo ExpectedDirectory { get; } = GetExpectedDirectory(SampleDirectory, OperationName, TestName);

    private static DirectoryInfo GetExpectedDirectory(DirectoryInfo sampleDirectory, string operationName, string testName)
    {
        return new DirectoryInfo(Path.Combine(sampleDirectory.FullName, $"expected.{operationName}.{testName}"));
    }

    private static DirectoryInfo GetRoot(DirectoryInfo sampleDirectory, string rootRelativePath)
    {
        return new DirectoryInfo(Path.Combine(sampleDirectory.FullName, rootRelativePath));
    }

    // TODO: Customize how TestDescriptor instances are showed in test results
}

public record RestoreTestDescriptor(DirectoryInfo SampleDirectory, string RootRelativePath, string TestName, RestoreCommandInputs CommandInputs, int ExitCode = 0)
    : TestDescriptor(SampleDirectory, RootRelativePath, "restore", TestName, ExitCode)
{
}

public record RestoreCommandInputs(string ProjectOrSolution);

public sealed class RestoreTable
{
    public string? TestName { get; set; }
    public string? RootDirectory { get; set; }
    public string? ProjectOrSolution { get; set; }
    public int? ExitCode { get; set; }
}
