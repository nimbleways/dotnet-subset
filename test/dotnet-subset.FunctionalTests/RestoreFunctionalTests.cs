using Nimbleways.Tools.Subset.Helpers;
using Nimbleways.Tools.Subset.Models;
using Nimbleways.Tools.Subset.Utils;

namespace Nimbleways.Tools.Subset;

public class RestoreFunctionalTests : IDisposable
{
    private static readonly IReadOnlyCollection<TestDescriptor> TestDescriptors = TestHelpers.GetTestDescriptors();
    private readonly DisposableTempDirectory _tempDirectory = new();
    private bool _disposedValue;

    private DirectoryInfo OutputDirectory => _tempDirectory.Value;
    public static IEnumerable<object[]> GetRestoreTestDescriptors()
    {
        object[][] objects = TestDescriptors.OfType<RestoreTestDescriptor>().Select(rtd => new object[] { rtd }).ToArray();
        return objects;
    }

    [Theory]
    [MemberData(nameof(GetRestoreTestDescriptors))]
    public void RunRestoreTests(RestoreTestDescriptor restoreTestDescriptor)
    {
        DotnetSubsetRunner.Run(restoreTestDescriptor, OutputDirectory);
        Assert.True(DirectoryDiff.AreDirectoriesIdentical(restoreTestDescriptor.ExpectedDirectory, OutputDirectory));
    }

    [Fact]
    public void CanRunTwiceWithSameArguments()
    {
        var restoreTestDescriptor = TestDescriptors.OfType<RestoreTestDescriptor>().First();
        DotnetSubsetRunner.Run(restoreTestDescriptor, OutputDirectory);
        DotnetSubsetRunner.Run(restoreTestDescriptor, OutputDirectory);
        Assert.True(DirectoryDiff.AreDirectoriesIdentical(restoreTestDescriptor.ExpectedDirectory, OutputDirectory));
    }

    [Fact]
    public void FailsIfOutputContainsANonIdenticalFile()
    {
        var restoreTestDescriptor = TestDescriptors.OfType<RestoreTestDescriptor>().First();
        DotnetSubsetRunner.Run(restoreTestDescriptor, OutputDirectory);
        var fileInOutput = OutputDirectory.EnumerateFiles("*", SearchOption.AllDirectories).First(f => f.Length > 0);
        IncrementFileLastByteValue(fileInOutput);
        Assert.Throws<InvalidOperationException>(() => DotnetSubsetRunner.Run(restoreTestDescriptor, OutputDirectory));
    }

    [Fact]
    public void CopyMissingFilesInOutput()
    {
        var restoreTestDescriptor = TestDescriptors.OfType<RestoreTestDescriptor>().First();
        DotnetSubsetRunner.Run(restoreTestDescriptor, OutputDirectory);
        DeleteHalfTheFiles(OutputDirectory);
        DotnetSubsetRunner.Run(restoreTestDescriptor, OutputDirectory);
        Assert.True(DirectoryDiff.AreDirectoriesIdentical(restoreTestDescriptor.ExpectedDirectory, OutputDirectory));
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
