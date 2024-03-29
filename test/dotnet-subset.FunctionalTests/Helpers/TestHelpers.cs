using System.Collections.Immutable;
using System.Reflection;

using Nimbleways.Tools.Subset.Models;
using Nimbleways.Tools.Subset.Utils;

using Tomlyn;
using Tomlyn.Model;

namespace Nimbleways.Tools.Subset.Helpers;

internal static class TestHelpers
{
    public static readonly DirectoryInfo RepositoryRoot = GetRepositoryRoot();

    public static IReadOnlyCollection<TestDescriptor> GetTestDescriptors()
    {
        var testsTomlFiles = GetTestsTomlFiles();
        TestDescriptor[] testDescriptors = testsTomlFiles.SelectMany(GetTestDescriptors).ToArray();
        EnsureTestNamesUniqueness(testDescriptors);
        return testDescriptors;
    }

    private static DirectoryInfo GetRepositoryRoot()
    {
        Assembly currentAssembly = Assembly.GetExecutingAssembly();
        var repositoryRootRelativePathToAssembly = currentAssembly
            .GetCustomAttribute<AssemblyRepositoryRootAttribute>()
            .AsNotNull()
            .RepositoryRootRelativePathToAssembly;
        string assemblyPath = Path.GetDirectoryName(currentAssembly.Location).AsNotNull();
        DirectoryInfo repositoryRoot = new(Path.Combine(assemblyPath, repositoryRootRelativePathToAssembly));
        EnsureExists(repositoryRoot);
        return repositoryRoot;
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

    private static ImmutableArray<FileInfo> GetTestsTomlFiles()
    {
        DirectoryInfo resourcesDirectory = GetTestResourcesDirectory();
        EnsureExists(resourcesDirectory);
        return resourcesDirectory
            .EnumerateFiles("tests.toml", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                MaxRecursionDepth = 1,
            }).ToImmutableArray();
    }

    private static DirectoryInfo GetTestResourcesDirectory()
    {
        return new(Path.Combine(RepositoryRoot.FullName, "test", "_resources"));
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
            .Select(rt => new RestoreTestDescriptor(sampleDirectory, rt.RootDirectory ?? "root", rt.TestName.AsNotNull(), new RestoreCommandInputs(rt.ProjectOrSolution.AsNotNull()), rt.ExitCode ?? 0));
    }

    private static T ToModel<T>(TomlTable table) where T : class, new()
    {
        return Toml.ToModel<T>(Toml.FromModel(table));
    }
}
