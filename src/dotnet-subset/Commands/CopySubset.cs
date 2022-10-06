using Microsoft.Build.Evaluation;

namespace Nimbleways.Tools.Subset.Commands;

internal class CopySubset : SubsetCommandBase
{
    protected override IEnumerable<string> GetFilesToCopy(string projectOrSolution, string rootFolder)
    {
        using ProjectCollection projectCollection = new();
        Dictionary<string, Project> projectsByFullPath = new();
        foreach (string? project in GetRootProjects(projectOrSolution))
        {
            VisitAllProjects(projectCollection, rootFolder, project, projectsByFullPath);
        }

        string projectListAsString = string.Join(Environment.NewLine + " - ", projectsByFullPath.Keys.OrderBy(f => f));
        Console.WriteLine($"Found {projectsByFullPath.Count} project(s) to copy:{Environment.NewLine + " - "}{projectListAsString}");

        var projectFiles = GetProjectFiles(projectsByFullPath);

        List<string> extraFilesInvolvedInRestore = projectsByFullPath.Values
            .SelectMany(project => GetExtraFilesInvolvedInRestore(rootFolder, project))
            .Distinct()
            .ToList();
        if (IsSolutionFile(projectOrSolution))
        {
            extraFilesInvolvedInRestore.Add(projectOrSolution);
        }

        if (extraFilesInvolvedInRestore.Count > 0)
        {
            string extraFilesInvolvedInRestoreAsString = string.Join(Environment.NewLine + " - ", extraFilesInvolvedInRestore.OrderBy(f => f));
            Console.WriteLine($"Found {extraFilesInvolvedInRestore.Count} extra file(s) to copy:{Environment.NewLine + " - "}{extraFilesInvolvedInRestoreAsString}");
        }

        IEnumerable<string> allFilesToCopy = projectsByFullPath.Keys.Concat(extraFilesInvolvedInRestore).Distinct();
        return allFilesToCopy;
    }

    private static IEnumerable<string> GetProjectFiles(Dictionary<string, Project> projects)
    {
        return projects.Values.SelectMany(project => Directory.EnumerateFiles(project.DirectoryPath, "*.*", SearchOption.AllDirectories));
    }
}
