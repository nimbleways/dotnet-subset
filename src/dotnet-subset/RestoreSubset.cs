using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

using Nimbleways.Tools.Subset.Exceptions;

namespace Nimbleways.Tools.Subset;
internal static class RestoreSubset
{
    public static void Execute(FileInfo projectOrSolution, DirectoryInfo rootFolder, DirectoryInfo destinationFolder)
    {
        if (!IsSameOrUnder(rootFolder, projectOrSolution))
        {
            throw new InvalidRootDirectoryException(projectOrSolution, rootFolder);
        }
        destinationFolder.Create();

        using var projectCollection = new ProjectCollection();
        var projectsByFullPath = new Dictionary<string, Project>();
        foreach (var project in GetRootProjects(projectOrSolution))
        {
            VisitAllProjects(projectCollection, rootFolder, project, projectsByFullPath);
        }
        var projectListAsString = string.Join(Environment.NewLine + " - ", projectsByFullPath.Keys.OrderBy(f => f));
        Console.WriteLine($"Found {projectsByFullPath.Count} project(s) to copy:{Environment.NewLine + " - "}{projectListAsString}");
        Console.WriteLine();
        var nugetConfigFiles = GetNugetConfigFiles(rootFolder, projectsByFullPath);
        var extraFilesInvolvedInRestore = projectsByFullPath.Values
            .SelectMany(project => GetExtraFilesInvolvedInRestore(rootFolder, project))
            .Concat(nugetConfigFiles)
            .Distinct(FileInfoComparer.Instance)
            .ToList();
        if (IsSolutionFile(projectOrSolution))
        {
            extraFilesInvolvedInRestore.Add(projectOrSolution);
        }
        if (extraFilesInvolvedInRestore.Count > 0)
        {
            var extraFilesInvolvedInRestoreAsString = string.Join(Environment.NewLine + " - ", extraFilesInvolvedInRestore.Select(f => f.FullName).OrderBy(f => f));
            Console.WriteLine($"Found {extraFilesInvolvedInRestore.Count} extra file(s) to copy:{Environment.NewLine + " - "}{extraFilesInvolvedInRestoreAsString}");
            Console.WriteLine();
        }
        var allFilesToCopy = projectsByFullPath.Keys
            .Select(fullPath => new FileInfo(fullPath))
            .Concat(extraFilesInvolvedInRestore)
            .Distinct(FileInfoComparer.Instance);

        int allFilesCount = 0;
        int copiedFilesCount = 0;
        foreach (var file in allFilesToCopy)
        {
            ++allFilesCount;
            var destinationFile = WithNewRoot(file, rootFolder, destinationFolder);
            destinationFile.Directory.Create();
            if (!destinationFile.Exists)
            {
                file.CopyTo(destinationFile.FullName);
                ++copiedFilesCount;
            }
            else if (!AreFilesIdentical(file, destinationFile))
            {
                throw new DestinationFileAlreadyExistsAndNotIdenticalException(file, destinationFile);
            }
        }
        Console.WriteLine($"Copied {copiedFilesCount} file(s) to '{destinationFolder.FullName}'. {allFilesCount - copiedFilesCount} file(s) already exist in destination.");
    }

    private static FileInfo WithNewRoot(FileInfo file, DirectoryInfo oldRoot, DirectoryInfo newRoot)
    {
        return new FileInfo(Path.Combine(newRoot.FullName, Path.GetRelativePath(oldRoot.FullName, file.FullName)));
    }

    private static IEnumerable<FileInfo> GetRootProjects(FileInfo projectOrSolution)
    {
        if (IsSolutionFile(projectOrSolution))
        {
            var solution = SolutionFile.Parse(projectOrSolution.FullName);
            return solution.ProjectsInOrder
                .Where(p => p.ProjectType != SolutionProjectType.SolutionFolder)
                .Select(p => new FileInfo(p.AbsolutePath));
        }
        return new[] { projectOrSolution };
    }

    private static bool IsSolutionFile(FileInfo projectOrSolution)
    {
        return ".sln".Equals(projectOrSolution.Extension, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameOrUnder(DirectoryInfo rootFolder, FileSystemInfo path)
    {
        var relativePath = Path.GetRelativePath(rootFolder.FullName, path.FullName);
        return relativePath != ".." && !relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !Path.IsPathRooted(relativePath);
    }

    private static FileInfo GetFullPathWithOriginalCase(FileInfo path)
    {
        return !path.Exists ? path : path.Directory.EnumerateFiles(path.Name).First();
    }

    private static FileInfo GetFileInfo(string relativeOrFullPath, DirectoryInfo? basePath = null)
    {
        var fullFilePath = basePath != null ? Path.GetFullPath(relativeOrFullPath, basePath.FullName) : Path.GetFullPath(relativeOrFullPath);
        return new FileInfo(fullFilePath);
    }

    private static void VisitAllProjects(ProjectCollection projectCollection, DirectoryInfo rootFolder, FileInfo projectPath, Dictionary<string, Project> projects)
    {
        var projectFileInfo = GetFullPathWithOriginalCase(projectPath);
        if (projects.ContainsKey(projectFileInfo.FullName) || !IsSameOrUnder(rootFolder, projectFileInfo))
        {
            return;
        }

        var project = projectCollection.LoadProject(projectFileInfo.FullName);
        projects.Add(projectFileInfo.FullName, project);
        var r = project.GetItems("ProjectReference");
        foreach (var projectReference in r)
        {
            var referenceFileInfo = GetFileInfo(projectReference.EvaluatedInclude, projectFileInfo.Directory);
            VisitAllProjects(projectCollection, rootFolder, referenceFileInfo, projects);
        }
    }

    private static FileInfo? GetPackagesLockFile(DirectoryInfo projectFolder, Project project)
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
            .Select(name => GetFullPathWithOriginalCase(GetFileInfo(name, projectFolder)))
            .FirstOrDefault(f => f.Exists);
    }
    private static IEnumerable<FileInfo> GetExtraFilesInvolvedInRestore(DirectoryInfo rootFolder, Project project)
    {
        var projectFolder = Path.GetDirectoryName(project.FullPath);
        var packagesLockFile = GetPackagesLockFile(new DirectoryInfo(projectFolder), project);
        if (packagesLockFile is not null)
        {
            yield return packagesLockFile;
        }

        foreach (var importedFileInfo in project.Imports.Select(i => new FileInfo(i.ImportedProject.FullPath)))
        {
            if (IsSameOrUnder(rootFolder, importedFileInfo))
            {
                yield return importedFileInfo;
            }
        }
    }
    private static IEnumerable<FileInfo> GetNugetConfigFiles(DirectoryInfo rootFolder, Dictionary<string, Project> projects)
    {
        static void GetNugetConfigFiles(DirectoryInfo rootFolder, DirectoryInfo? folder, IDictionary<string, FileInfo> nugetConfigFiles)
        {
            if (folder == null || nugetConfigFiles.ContainsKey(folder.FullName) || !IsSameOrUnder(rootFolder, folder))
            {
                return;
            }

            var f = new[] { "nuget.config", "NuGet.config", "NuGet.Config" }
                .Select(name => GetFullPathWithOriginalCase(GetFileInfo(name, folder)))
                .FirstOrDefault(f => f.Exists);
            if (f is not null)
            {
                nugetConfigFiles.Add(folder.FullName, f);
            }

            GetNugetConfigFiles(rootFolder, folder.Parent, nugetConfigFiles);
        }
        var nugetConfigFilesByFolder = new Dictionary<string, FileInfo>();
        var csprojDefinedNugetConfigFiles = new List<FileInfo>();
        foreach (var (projectPath, project) in projects)
        {
            var restoreConfigFilePropertyValue = project.GetPropertyValue("RestoreConfigFile");
            if (!string.IsNullOrEmpty(restoreConfigFilePropertyValue))
            {
                var fullPath = GetFullPathWithOriginalCase(GetFileInfo(restoreConfigFilePropertyValue));
                if (IsSameOrUnder(rootFolder, fullPath))
                {
                    if (fullPath.Exists)
                    {
                        csprojDefinedNugetConfigFiles.Add(fullPath);
                    }
                    else
                    {
                        throw new RestoreConfigFileNotFoundException(projectPath, restoreConfigFilePropertyValue);
                    }
                }
            }
            else
            {
                GetNugetConfigFiles(rootFolder, Directory.GetParent(projectPath), nugetConfigFilesByFolder);
            }
        }
        return nugetConfigFilesByFolder.Values.Concat(csprojDefinedNugetConfigFiles);
    }
    private static bool AreFilesIdentical(FileInfo file1, FileInfo file2)
    {
        const int bytesToRead = 1024 * 10;

        using FileStream fs1 = file1.Open(FileMode.Open);
        using FileStream fs2 = file2.Open(FileMode.Open);

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
