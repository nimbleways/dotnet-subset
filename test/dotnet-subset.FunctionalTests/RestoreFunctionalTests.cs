using System.Text.RegularExpressions;

using Nimbleways.Tools.Subset.Exceptions;
using Nimbleways.Tools.Subset.Helpers;
using Nimbleways.Tools.Subset.Models;
using Nimbleways.Tools.Subset.Utils;

using static Nimbleways.Tools.Subset.Helpers.DotnetSubsetRunner;

namespace Nimbleways.Tools.Subset;

[UsesVerify]
public class RestoreFunctionalTests : IDisposable
{
    private static readonly IReadOnlyCollection<TestDescriptor> AllTestDescriptors = TestHelpers.GetTestDescriptors();

    private readonly DisposableTempDirectory _tempDirectory = new();
    private bool _disposedValue;

    private DirectoryInfo OutputDirectory => _tempDirectory.Value;

    public static IEnumerable<object[]> GetRestoreTestDescriptors()
    {
        object[][] objects = AllTestDescriptors
            .OfType<RestoreTestDescriptor>()
            .Select(rtd => new object[] { rtd }).ToArray();
        return objects;
    }

    [Theory]
    [MemberData(nameof(GetRestoreTestDescriptors))]
    public async Task RunRestoreTests(RestoreTestDescriptor restoreTestDescriptor)
    {
        await AssertDescriptor(restoreTestDescriptor, OutputDirectory).VerifyOutput();
        if (restoreTestDescriptor.ExitCode == 0)
        {
            Assert.True(DirectoryDiff.AreDirectoriesIdentical(restoreTestDescriptor.ExpectedDirectory, OutputDirectory));
        }
    }

    [Fact]
    public async Task CanRunTwiceWithSameArguments()
    {
        var restoreTestDescriptor = GetProjectWithOneDependencyRestoreTestDescriptor();
        AssertDescriptor(restoreTestDescriptor, OutputDirectory);
        await AssertDescriptor(restoreTestDescriptor, OutputDirectory).VerifyOutput();
        Assert.True(DirectoryDiff.AreDirectoriesIdentical(restoreTestDescriptor.ExpectedDirectory, OutputDirectory));
    }

    [Fact]
    public async Task FailsIfOutputContainsANonIdenticalFileWithSameSize()
    {
        var restoreTestDescriptor = GetProjectWithOneDependencyRestoreTestDescriptor();
        AssertDescriptor(restoreTestDescriptor, OutputDirectory);
        var fileInOutput = OutputDirectory.EnumerateFiles("CompanyName.MyMeetings.BuildingBlocks.Application.csproj", SearchOption.AllDirectories).Single();
        IncrementFileLastByteValue(fileInOutput);
        await AssertDescriptor(restoreTestDescriptor, OutputDirectory, DestinationFileAlreadyExistsAndNotIdenticalException.EXIT_CODE).VerifyOutput();
    }

    [Fact]
    public async Task FailsIfOutputContainsANonIdenticalFileWithDifferentSize()
    {
        var restoreTestDescriptor = GetProjectWithOneDependencyRestoreTestDescriptor();
        AssertDescriptor(restoreTestDescriptor, OutputDirectory);
        var fileInOutput = OutputDirectory.EnumerateFiles("CompanyName.MyMeetings.BuildingBlocks.Application.csproj", SearchOption.AllDirectories).Single();
        using (var streamWriter = File.AppendText(fileInOutput.FullName))
        {
            streamWriter.Write(Guid.NewGuid().ToByteArray());
        }

        await AssertDescriptor(restoreTestDescriptor, OutputDirectory, DestinationFileAlreadyExistsAndNotIdenticalException.EXIT_CODE).VerifyOutput();
    }

    [Fact]
    public async Task CopyMissingFilesInOutput()
    {
        var restoreTestDescriptor = GetProjectWithOneDependencyRestoreTestDescriptor();
        AssertDescriptor(restoreTestDescriptor, OutputDirectory);
        DeleteHalfTheFiles(OutputDirectory);
        await AssertDescriptor(restoreTestDescriptor, OutputDirectory).VerifyOutput();
        Assert.True(DirectoryDiff.AreDirectoriesIdentical(restoreTestDescriptor.ExpectedDirectory, OutputDirectory));
    }

    [Fact]
    public async Task Return255ForUnexpectedErrors()
    {
        var restoreTestDescriptor = GetProjectWithOneDependencyRestoreTestDescriptor();
        AssertDescriptor(restoreTestDescriptor, OutputDirectory);
        var fileInOutput = OutputDirectory.EnumerateFiles("CompanyName.MyMeetings.BuildingBlocks.Application.csproj", SearchOption.AllDirectories).Single();
        using Stream _ = GetExclusiveReadStream(fileInOutput);
        await AssertDescriptor(restoreTestDescriptor, OutputDirectory, 255).VerifyOutput();
    }

    [Fact]
    public async Task FailWhenProjectIsNotUnderRootDirectory()
    {
        var rootDir = new DirectoryInfo(Path.Combine(OutputDirectory.FullName, "SampleDir", "root"));
        rootDir.Create();
        var projectFile = new FileInfo(Path.Combine(OutputDirectory.FullName, "project.csproj"));
        var restoreTestDescriptor = new RestoreTestDescriptor(rootDir.Parent.AsNotNull(), "test", new RestoreCommandInputs(projectFile.FullName));
        await AssertDescriptor(restoreTestDescriptor, OutputDirectory, InvalidRootDirectoryException.EXIT_CODE).VerifyOutput();
    }

    [Fact]
    public async Task PrintApplicationAndRuntimeVersionsInFirstLineWhenNoLogoIsFalse()
    {
        var restoreTestDescriptor = GetProjectWithOneDependencyRestoreTestDescriptor();
        await AssertDescriptor(restoreTestDescriptor, OutputDirectory, noLogo: false)
            .VerifyOutput(
            t => t.ScrubLinesWithReplace(line => Regex.Replace(line, @"^\{ApplicationName\} (\d+\.){2}\d+ \(\.NET Runtime (\d+\.){2}\d+\)$", "{ApplicationName} {ApplicationVersion} (.NET Runtime {DotNETRuntimeVersion})")));
    }

    private static FileStream GetExclusiveReadStream(FileInfo fileInOutput)
    {
        return fileInOutput.Open(FileMode.Open, FileAccess.Read, FileShare.None);
    }

    private static RestoreTestDescriptor GetProjectWithOneDependencyRestoreTestDescriptor()
    {
        return AllTestDescriptors.OfType<RestoreTestDescriptor>().First(rtd => rtd.TestName == "project_with_one_dependency");
    }

    private static void DeleteHalfTheFiles(DirectoryInfo directory)
    {
        var outputFiles = directory.EnumerateFiles("*", SearchOption.AllDirectories).ToArray();
        if (outputFiles.Length < 2)
        {
            throw new InvalidOperationException();
        }

        foreach (var file in outputFiles.Take(outputFiles.Length / 2))
        {
            file.Delete();
        }
    }

    private static void IncrementFileLastByteValue(FileInfo fileInOutput)
    {
        using var fileStream = fileInOutput.Open(FileMode.Open, FileAccess.ReadWrite);
        fileStream.Position = fileInOutput.Length - 1;
        var lastByte = fileStream.ReadByte();
        fileStream.Position = fileInOutput.Length - 1;
        byte incrementedByte = (byte)(lastByte == byte.MaxValue ? 0 : lastByte + 1);
        fileStream.WriteByte(incrementedByte);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _tempDirectory.Dispose();
            }
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
