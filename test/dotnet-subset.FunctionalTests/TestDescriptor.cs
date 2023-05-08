using System.Runtime.CompilerServices;

using Tomlyn;
using Tomlyn.Model;

namespace Nimbleways.Tools.Subset;

public abstract record TestDescriptor(DirectoryInfo SampleDirectory, string? TestName);

public record RestoreTestDescriptor(DirectoryInfo SampleDirectory, string? TestName, RestoreCommandInputs CommandInputs)
    : TestDescriptor(SampleDirectory, TestName)
{
    public DirectoryInfo ExpectedDirectory { get; } = GetExpectedDirectory(SampleDirectory, TestName);

    private static DirectoryInfo GetExpectedDirectory(DirectoryInfo sampleDirectory, string? testName)
    {
        return testName is null
            ? new DirectoryInfo(Path.Combine(sampleDirectory.FullName, "expected.restore"))
            : new DirectoryInfo(Path.Combine(sampleDirectory.FullName, $"expected.restore.{testName}"));
    }
}

public record RestoreCommandInputs(string ProjectOrSolution);

internal static class TestUtils
{
    public static IReadOnlyCollection<TestDescriptor> GetTestDescriptors()
    {
        var testsTomlFiles = GetTestsTomlFiles();
        return testsTomlFiles.SelectMany(GetTestDescriptors).ToArray();
    }

    private static IReadOnlyCollection<FileInfo> GetTestsTomlFiles()
    {
        var thisDir = Directory.GetParent(GetThisFilePath()).AsNotNull();
        return new DirectoryInfo(Path.Combine(thisDir.FullName, "..", ".resources"))
            .EnumerateFiles("tests.toml", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                MaxRecursionDepth = 1,
            }).ToArray();

        static string GetThisFilePath([CallerFilePath] string filePath = null!)
        {
            return filePath;
        }
    }

    private static IReadOnlyCollection<TestDescriptor> GetTestDescriptors(FileInfo testsTomlFile)
    {
        var descriptors = new List<TestDescriptor>();
        var rootTable = Toml.ToModel(File.ReadAllText(testsTomlFile.FullName), testsTomlFile.FullName);
        DirectoryInfo sampleDirectory = testsTomlFile.Directory.AsNotNull();
        foreach (var (key, value) in rootTable)
        {
            if (value is not TomlTableArray tables)
            {
                continue;
            }

            descriptors.AddRange(key switch
            {
                "restore" => GetRestoreDescriptors(tables, sampleDirectory),
                _ => throw new NotSupportedException($"Table name '{key}' is not supported"),
            });
        }
        return descriptors.AsReadOnly();
    }

    private static IEnumerable<RestoreTestDescriptor> GetRestoreDescriptors(TomlTableArray tables, DirectoryInfo sampleDirectory)
    {
        return tables.Select(ToModel<RestoreTable>)
            .Select(rt => new RestoreTestDescriptor(sampleDirectory, rt.TestName, new RestoreCommandInputs(rt.ProjectOrSolution.AsNotNull())));
    }

    private static T ToModel<T>(TomlTable table) where T : class, new()
    {
        return Toml.ToModel<T>(Toml.FromModel(table));
    }
}

public sealed class RestoreTable
{
    public string? TestName { get; set; }
    public string? ProjectOrSolution { get; set; }
}
