// https://www.daveaglick.com/posts/running-a-design-time-build-with-msbuild-apis
// https://docs.microsoft.com/en-us/visualstudio/msbuild/updating-an-existing-application?view=vs-2022#use-microsoftbuildlocator

using System.CommandLine;

using Microsoft.Build.Locator;

using Nimbleways.Tools.Subset;

MSBuildLocator.RegisterDefaults();

var projectArgument = new Argument<FileInfo>(
    name: "project",
    description: "Project to restore.");

var rootDirectoryOption = new Option<DirectoryInfo>(
    name: "--root-directory",
    description: "Directory from where the files will be copied, usually the repository's root.")
{ IsRequired = true };

var outputDirectoryOption = new Option<DirectoryInfo>(
    name: "--output",
    description: "Directory where the subset files will be copied, preserving the original hierarchy.")
{ IsRequired = true };

var rootCommand = new RootCommand(".NET Tool to copy a subset of files from a repository to a directory.");

var readCommand = new Command("restore", "Create a subset for the restore operation.")
            {
                rootDirectoryOption,
                outputDirectoryOption,
            };
readCommand.AddArgument(projectArgument);
rootCommand.AddCommand(readCommand);

readCommand.SetHandler((project, rootDirectory, outputDirectory) => RestoreSubset.Execute(project.FullName, rootDirectory.FullName, outputDirectory.FullName),
    projectArgument, rootDirectoryOption, outputDirectoryOption);

return rootCommand.Invoke(args);
