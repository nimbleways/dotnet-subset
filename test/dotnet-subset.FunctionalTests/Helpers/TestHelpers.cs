using System.Runtime.CompilerServices;

using Nimbleways.Tools.Subset.Models;
using Nimbleways.Tools.Subset.Utils;

using Tomlyn;
using Tomlyn.Model;

namespace Nimbleways.Tools.Subset.Helpers;

internal static class TestHelpers
{
    public static IReadOnlyCollection<TestDescriptor> GetTestDescriptors()
    {
        var testsTomlFiles = GetTestsTomlFiles();
        TestDescriptor[] testDescriptors = testsTomlFiles.SelectMany(GetTestDescriptors).ToArray();
        EnsureTestNamesUniqueness(testDescriptors);
        return testDescriptors;
    }

    private static void EnsureTestNamesUniqueness(TestDescriptor[] testDescriptors)
    {
        HashSet<string> testNames = new();
        foreach (var testDescriptor in testDescriptors)
        {
            string testNameWithOperationName = $"{testDescriptor.OperationName}: {testDescriptor.TestName}";
            if (!testNames.Add(testNameWithOperationName))
            {
                throw new InvalidOperationException($"'{testNameWithOperationName}' is not unique");
            }
        }
    }

    private static IReadOnlyCollection<FileInfo> GetTestsTomlFiles()
    {
        DirectoryInfo resourcesDirectory = GetTestResourcesDirectory();
        EnsureExists(resourcesDirectory);
        return resourcesDirectory
            .EnumerateFiles("tests.toml", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                MaxRecursionDepth = 1,
            }).ToArray();
    }

    private static DirectoryInfo GetTestResourcesDirectory()
    {
        if (Environment.GetEnvironmentVariable("CI_DOTNET_SUBSET_TEST_RESOURCES_DIR") is { } path)
        {
            Console.WriteLine("Using CI_DOTNET_SUBSET_TEST_RESOURCES_DIR");
            return new(path);
        }
        var thisDir = Directory.GetParent(GetThisFilePath()).AsNotNull();
        return new(Path.Combine(thisDir.FullName, "..", "..", "_resources"));

        static string GetThisFilePath([CallerFilePath] string filePath = null!)
        {
            return filePath;
        }
    }

    private static void EnsureExists(DirectoryInfo directoryInfo)
    {
        if (directoryInfo.Exists)
        {
            return;
        }
        DirectoryInfo? parent = directoryInfo.Parent;
        while (parent != null)
        {
            if (parent.Exists)
            {
                break;
            }
            parent = parent.Parent;
        }
        string existingParentMessage = parent == null ? string.Empty : $" The nearest existing directory is {parent.FullName}";
        throw new DirectoryNotFoundException($"Directory {directoryInfo.FullName} does not exist.{existingParentMessage}");
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
            .Select(rt => new RestoreTestDescriptor(sampleDirectory, rt.TestName.AsNotNull(), new RestoreCommandInputs(rt.ProjectOrSolution.AsNotNull()), rt.ExitCode ?? 0));
    }

    private static T ToModel<T>(TomlTable table) where T : class, new()
    {
        return Toml.ToModel<T>(Toml.FromModel(table));
    }
}
