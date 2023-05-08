using System.Diagnostics;

namespace Nimbleways.Tools.Subset;

public class RestoreFunctionalTests
{
    public static IEnumerable<object[]> GetRestoreTestDescriptors()
    {
        return TestUtils.GetTestDescriptors().OfType<RestoreTestDescriptor>().Select(rtd => new object[] { rtd });
    }

    [Theory]
    [MemberData(nameof(GetRestoreTestDescriptors))]
    public void RunRestoreTests(RestoreTestDescriptor restoreTestDescriptor)
    {
        DirectoryInfo outputDirectory = Run(restoreTestDescriptor);
        Assert.True(DirectoryDiff.AreDirectoriesIdentical(restoreTestDescriptor.ExpectedDirectory, outputDirectory));
    }

    private static DirectoryInfo Run(RestoreTestDescriptor restoreTestDescriptor)
    {
        string projectOrSolution = Path.Combine(restoreTestDescriptor.SampleDirectory.FullName, "root", restoreTestDescriptor.CommandInputs.ProjectOrSolution);
        var output = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        string root = Path.Combine(restoreTestDescriptor.SampleDirectory.FullName, "root");

        using Process process = new();

        // Configure the process
        process.StartInfo.FileName = "dotnet";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.Arguments = $@"subset restore ""{projectOrSolution}"" --output ""{output.FullName}"" --root-directory ""{root}""";

        // Start the process
        process.Start();

        // Wait for the process to exit
        process.WaitForExit();

        return process.ExitCode == 0 ?
            output
            : throw new InvalidOperationException($"process.ExitCode=={process.ExitCode}. Expected 0.");
    }
}
