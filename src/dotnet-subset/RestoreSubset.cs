using Microsoft.Build.Evaluation;

namespace Nimbleways.Tools.Subset;
internal static class RestoreSubset
{
    public static void Execute(string mainProjectPath, string rootFolder, string destinationFolder)
    {
        if (!IsSameOrUnder(rootFolder, mainProjectPath))
        {
            throw new ArgumentException($"Project '${mainProjectPath}' must be under the root folder '{rootFolder}'");
        }
        Directory.CreateDirectory(destinationFolder);

        using var projectCollection = new ProjectCollection();
        var projectsByFullPath = new Dictionary<string, Project>();
        VisitAllProjects(projectCollection, rootFolder, mainProjectPath, projectsByFullPath);
        var projectListAsString = string.Join(Environment.NewLine + " - ", projectsByFullPath.Keys.OrderBy(f => f));
        Console.WriteLine($"Found {projectsByFullPath.Count} project(s) to copy:{Environment.NewLine + " - "}{projectListAsString}");
        var nugetConfigFiles = GetNugetConfigFiles(rootFolder, projectsByFullPath);
        var extraFilesInvolvedInRestore = projectsByFullPath.Values
            .SelectMany(project => GetExtraFilesInvolvedInRestore(rootFolder, project))
            .Concat(nugetConfigFiles)
            .Distinct()
            .ToArray();
        if (extraFilesInvolvedInRestore.Length > 0)
        {
            var extraFilesInvolvedInRestoreAsString = string.Join(Environment.NewLine + " - ", extraFilesInvolvedInRestore.OrderBy(f => f));
            Console.WriteLine($"Found {extraFilesInvolvedInRestore.Length} extra file(s) to copy:{Environment.NewLine + " - "}{extraFilesInvolvedInRestoreAsString}");
        }
        var allFilesToCopy = projectsByFullPath.Keys.Concat(extraFilesInvolvedInRestore).Distinct();

        int allFilesCount = 0;
        int copiedFilesCount = 0;
        foreach (var file in allFilesToCopy)
        {
            ++allFilesCount;
            var destinationFile = Path.Combine(destinationFolder, Path.GetRelativePath(rootFolder, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            if (!File.Exists(destinationFile))
            {
                File.Copy(file, destinationFile);
                ++copiedFilesCount;
            }
            else if (!AreFilesIdentical(file, destinationFile))
            {
                string errorMessage = $"ERROR: cannot copy '{file}' to '{destinationFile}': destination file already exist but have different content from source file";
                Console.WriteLine(errorMessage);
                throw new ArgumentException(errorMessage);
            }
        }
        Console.WriteLine($"Copied {copiedFilesCount} file(s) to '{destinationFolder}'. {allFilesCount - copiedFilesCount} file(s) already exist in destination.");
    }

    private static bool IsSameOrUnder(string rootFolder, string path)
    {
        var relativePath = Path.GetRelativePath(rootFolder, path);
        return relativePath != ".." && !relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !Path.IsPathRooted(relativePath);
    }

    private static string GetFullPathWithOriginalCase(string path, string? basePath = null)
    {
        var fullFilePath = basePath != null ? Path.GetFullPath(path, basePath) : Path.GetFullPath(path);
        if (!File.Exists(fullFilePath))
        {
            return fullFilePath;
        }

        var folder = Path.GetDirectoryName(fullFilePath) ?? throw new ArgumentException("GetDirectoryName returns null", nameof(path));
        return Directory.EnumerateFiles(folder, Path.GetFileName(fullFilePath)).First();
    }

    private static void VisitAllProjects(ProjectCollection projectCollection, string rootFolder, string projectPath, Dictionary<string, Project> projects)
    {
        if (!Path.IsPathRooted(projectPath))
        {
            throw new ArgumentException($"projectPath must be absolute: '{projectPath}");
        }
        var fullPath = GetFullPathWithOriginalCase(projectPath);
        if (projects.ContainsKey(fullPath) || !IsSameOrUnder(rootFolder, fullPath))
        {
            return;
        }

        var project = projectCollection.LoadProject(fullPath);
        projects.Add(fullPath, project);
        var r = project.GetItems("ProjectReference");
        foreach (var projectReference in r)
        {
            var path = Path.IsPathRooted(projectReference.EvaluatedInclude) ? projectReference.EvaluatedInclude : Path.Combine(Path.GetDirectoryName(fullPath)!, projectReference.EvaluatedInclude);
            VisitAllProjects(projectCollection, rootFolder, path, projects);
        }
    }

    private static string? GetPackagesLockFile(string projectFolder, Project project)
    {
        var candidateFilenames = new List<string>();
        var nuGetLockFilePathPropertyValue = project.GetPropertyValue("NuGetLockFilePath");
        if (!string.IsNullOrEmpty(nuGetLockFilePathPropertyValue))
        {
            candidateFilenames.Add(nuGetLockFilePathPropertyValue);
        }
        var projectNameWithoutSpaces = Path
            .GetFileNameWithoutExtension(project.FullPath)
            .Replace(" ", "_", StringComparison.OrdinalIgnoreCase);
        candidateFilenames.Add($"packages.{projectNameWithoutSpaces}.lock.json");
        candidateFilenames.Add("packages.lock.json");
        return candidateFilenames
            .Select(name => GetFullPathWithOriginalCase(name, projectFolder))
            .FirstOrDefault(File.Exists);
    }
    private static IEnumerable<string> GetExtraFilesInvolvedInRestore(string rootFolder, Project project)
    {
        var projectFolder = Path.GetDirectoryName(project.FullPath);
        if (projectFolder is null || !Directory.Exists(projectFolder))
        {
            throw new ArgumentException($"'{rootFolder}' doesn't exist");
        }

        var packagesLockFile = GetPackagesLockFile(projectFolder, project);
        if (packagesLockFile is not null)
        {
            yield return packagesLockFile;
        }

        foreach (var import in project.Imports)
        {
            if (IsSameOrUnder(rootFolder, import.ImportedProject.FullPath))
            {
                yield return import.ImportedProject.FullPath;
            }
        }
    }
    private static IEnumerable<string> GetNugetConfigFiles(string rootFolder, Dictionary<string, Project> projects)
    {
        static void GetNugetConfigFiles(string rootFolder, DirectoryInfo folder, IDictionary<string, string> nugetConfigFiles)
        {
            if (nugetConfigFiles.ContainsKey(folder.FullName) || !IsSameOrUnder(rootFolder, folder.FullName))
            {
                return;
            }

            var f = new[] { "nuget.config", "NuGet.config", "NuGet.Config" }
                .Select(name => GetFullPathWithOriginalCase(name, folder.FullName))
                .FirstOrDefault(File.Exists);
            if (f is not null)
            {
                nugetConfigFiles.Add(folder.FullName, f);
            }

            GetNugetConfigFiles(rootFolder, folder.Parent!, nugetConfigFiles);
        }
        var nugetConfigFilesByFolder = new Dictionary<string, string>();
        var csprojDefinedNugetConfigFiles = new List<string>();
        foreach (var (projectPath, project) in projects)
        {
            var restoreConfigFilePropertyValue = project.GetPropertyValue("RestoreConfigFile");
            if (!string.IsNullOrEmpty(restoreConfigFilePropertyValue))
            {
                var fullPath = GetFullPathWithOriginalCase(restoreConfigFilePropertyValue);
                if (IsSameOrUnder(rootFolder, fullPath))
                {
                    if (File.Exists(fullPath))
                    {
                        csprojDefinedNugetConfigFiles.Add(fullPath);
                    }
                    else
                    {
                        string errorMessage = $"ERROR: cannot find the file '{restoreConfigFilePropertyValue}' defined in the property 'RestoreConfigFile' of the project '{projectPath}'";
                        errorMessage += $"{Environment.NewLine}If the path is relative, it is resolved by NuGet against the current working directy, not the project directory";
                        Console.WriteLine(errorMessage);
                        throw new ArgumentException(errorMessage);
                    }
                }
            }
            else
            {
                GetNugetConfigFiles(rootFolder, Directory.GetParent(projectPath)!, nugetConfigFilesByFolder);
            }
        }
        return nugetConfigFilesByFolder.Values.Concat(csprojDefinedNugetConfigFiles);
    }
    private static bool AreFilesIdentical(string file1, string file2)
    {
        const int bytesToRead = 1024 * 10;

        using FileStream fs1 = File.Open(file1, FileMode.Open);
        using FileStream fs2 = File.Open(file2, FileMode.Open);

        if (fs1.Length != fs2.Length)
        {
            return false;
        }

        var buffer1 = new byte[bytesToRead];
        var buffer2 = new byte[bytesToRead];
        var buffer1ReadOnlySpan = (ReadOnlySpan<byte>)buffer1;
        var buffer2ReadOnlySpan = (ReadOnlySpan<byte>)buffer2;
        int len1, len2;
        do
        {
            len1 = fs1.Read(buffer1, 0, bytesToRead);
            len2 = fs2.Read(buffer2, 0, bytesToRead);

            if (!buffer1ReadOnlySpan.SequenceEqual(buffer2ReadOnlySpan))
            {
                return false;
            }
        }
        while (len1 == 0 || len2 == 0);

        return true;
    }
}
