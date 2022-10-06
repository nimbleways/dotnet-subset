using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace Nimbleways.Tools.Subset.Commands;

internal abstract class SubsetCommandBase
{
    public void Execute(string projectOrSolution, string rootFolder, string destinationFolder)
    {
        if (!IsSameOrUnder(rootFolder, projectOrSolution))
        {
            throw new ArgumentException($"Project or solution '${projectOrSolution}' must be under the root folder '{rootFolder}'");
        }

        Directory.CreateDirectory(destinationFolder);

        IEnumerable<string> allFilesToCopy = GetFilesToCopy(projectOrSolution, rootFolder);

        CopyFiles(rootFolder, destinationFolder, allFilesToCopy);
    }

    protected abstract IEnumerable<string> GetFilesToCopy(string projectOrSolution, string rootFolder);

    private static void CopyFiles(string rootFolder, string destinationFolder, IEnumerable<string> allFilesToCopy)
    {
        int allFilesCount = 0;
        int copiedFilesCount = 0;
        foreach (string? file in allFilesToCopy)
        {
            ++allFilesCount;
            string? destinationFile = Path.Combine(destinationFolder, Path.GetRelativePath(rootFolder, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            if (!File.Exists(destinationFile))
            {
                File.Copy(file, destinationFile);
                ++copiedFilesCount;
            }
            else if (!AreFilesIdentical(file, destinationFile))
            {
                string errorMessage =
                    $"ERROR: cannot copy '{file}' to '{destinationFile}': destination file already exist but have different content from source file";
                Console.WriteLine(errorMessage);
                throw new ArgumentException(errorMessage);
            }
        }

        Console.WriteLine($"Copied {copiedFilesCount} file(s) to '{destinationFolder}'. {allFilesCount - copiedFilesCount} file(s) already exist in destination.");
    }

    protected static IEnumerable<string> GetRootProjects(string projectOrSolution)
    {
        if (IsSolutionFile(projectOrSolution))
        {
            SolutionFile? solution = SolutionFile.Parse(projectOrSolution);
            return solution.ProjectsInOrder
                .Where(p => p.ProjectType != SolutionProjectType.SolutionFolder)
                .Select(p => p.AbsolutePath);
        }

        return new[]
        {
            projectOrSolution
        };
    }

    protected static bool IsSolutionFile(string projectOrSolution)
    {
        return ".sln".Equals(Path.GetExtension(projectOrSolution), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameOrUnder(string rootFolder, string path)
    {
        string? relativePath = Path.GetRelativePath(rootFolder, path);
        return relativePath != ".." && !relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !Path.IsPathRooted(relativePath);
    }

    private static string GetFullPathWithOriginalCase(string path, string? basePath = null)
    {
        string? fullFilePath = basePath != null ? Path.GetFullPath(path, basePath) : Path.GetFullPath(path);
        if (!File.Exists(fullFilePath))
        {
            return fullFilePath;
        }

        string folder = Path.GetDirectoryName(fullFilePath) ?? throw new ArgumentException("GetDirectoryName returns null", nameof(path));
        return Directory.EnumerateFiles(folder, Path.GetFileName(fullFilePath)).First();
    }

    protected void VisitAllProjects(ProjectCollection projectCollection, string rootFolder, string projectPath, Dictionary<string, Project> projects)
    {
        if (!Path.IsPathRooted(projectPath))
        {
            throw new ArgumentException($"projectPath must be absolute: '{projectPath}");
        }

        string fullPath = GetFullPathWithOriginalCase(projectPath);
        if (projects.ContainsKey(fullPath) || !IsSameOrUnder(rootFolder, fullPath))
        {
            return;
        }

        Project project = projectCollection.LoadProject(fullPath);
        projects.Add(fullPath, project);
        ICollection<ProjectItem>? r = project.GetItems("ProjectReference");
        foreach (ProjectItem? projectReference in r)
        {
            string? path = Path.IsPathRooted(projectReference.EvaluatedInclude)
                ? projectReference.EvaluatedInclude
                : Path.Combine(Path.GetDirectoryName(fullPath)!, projectReference.EvaluatedInclude);
            VisitAllProjects(projectCollection, rootFolder, path, projects);
        }
    }

    private static string? GetPackagesLockFile(string projectFolder, Project project)
    {
        List<string> candidateFilenames = new();
        string? nuGetLockFilePathPropertyValue = project.GetPropertyValue("NuGetLockFilePath");
        if (!string.IsNullOrEmpty(nuGetLockFilePathPropertyValue))
        {
            candidateFilenames.Add(nuGetLockFilePathPropertyValue);
        }

        string? projectNameWithoutSpaces = Path
            .GetFileNameWithoutExtension(project.FullPath)
            .Replace(" ", "_", StringComparison.OrdinalIgnoreCase);
        candidateFilenames.Add($"packages.{projectNameWithoutSpaces}.lock.json");
        candidateFilenames.Add("packages.lock.json");
        return candidateFilenames
            .Select(name => GetFullPathWithOriginalCase(name, projectFolder))
            .FirstOrDefault(File.Exists);
    }

    protected static IEnumerable<string> GetExtraFilesInvolvedInRestore(string rootFolder, Project project)
    {
        string? projectFolder = Path.GetDirectoryName(project.FullPath);
        if (projectFolder is null || !Directory.Exists(projectFolder))
        {
            throw new ArgumentException($"'{rootFolder}' doesn't exist");
        }

        string? packagesLockFile = GetPackagesLockFile(projectFolder, project);
        if (packagesLockFile is not null)
        {
            yield return packagesLockFile;
        }

        foreach (ResolvedImport import in project.Imports)
        {
            if (IsSameOrUnder(rootFolder, import.ImportedProject.FullPath))
            {
                yield return import.ImportedProject.FullPath;
            }
        }
    }

    protected static IEnumerable<string> GetNugetConfigFiles(string rootFolder, Dictionary<string, Project> projects)
    {
        void GetNugetConfigFiles(string rootFolder, DirectoryInfo folder, IDictionary<string, string> nugetConfigFiles)
        {
            if (nugetConfigFiles.ContainsKey(folder.FullName) || !IsSameOrUnder(rootFolder, folder.FullName))
            {
                return;
            }

            string? f = new[]
                {
                    "nuget.config", "NuGet.config", "NuGet.Config"
                }
                .Select(name => GetFullPathWithOriginalCase(name, folder.FullName))
                .FirstOrDefault(File.Exists);
            if (f is not null)
            {
                nugetConfigFiles.Add(folder.FullName, f);
            }

            GetNugetConfigFiles(rootFolder, folder.Parent!, nugetConfigFiles);
        }

        Dictionary<string, string> nugetConfigFilesByFolder = new();
        List<string> csprojDefinedNugetConfigFiles = new();
        foreach ((string? projectPath, Project? project) in projects)
        {
            string? restoreConfigFilePropertyValue = project.GetPropertyValue("RestoreConfigFile");
            if (!string.IsNullOrEmpty(restoreConfigFilePropertyValue))
            {
                string fullPath = GetFullPathWithOriginalCase(restoreConfigFilePropertyValue);
                if (IsSameOrUnder(rootFolder, fullPath))
                {
                    if (File.Exists(fullPath))
                    {
                        csprojDefinedNugetConfigFiles.Add(fullPath);
                    }
                    else
                    {
                        string errorMessage =
                            $"ERROR: cannot find the file '{restoreConfigFilePropertyValue}' defined in the property 'RestoreConfigFile' of the project '{projectPath}'";
                        errorMessage +=
                            $"{Environment.NewLine}If the path is relative, it is resolved by NuGet against the current working directy, not the project directory";
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

        byte[] buffer1 = new byte[bytesToRead];
        byte[] buffer2 = new byte[bytesToRead];
        ReadOnlySpan<byte> buffer1ReadOnlySpan = (ReadOnlySpan<byte>)buffer1;
        ReadOnlySpan<byte> buffer2ReadOnlySpan = (ReadOnlySpan<byte>)buffer2;
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
