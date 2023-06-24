// https://www.daveaglick.com/posts/running-a-design-time-build-with-msbuild-apis
// https://docs.microsoft.com/en-us/visualstudio/msbuild/updating-an-existing-application?view=vs-2022#use-microsoftbuildlocator

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

using Microsoft.Build.Locator;

using Nimbleways.Tools.Subset.Exceptions;

namespace Nimbleways.Tools.Subset;

public static class Program
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>")]
    private static VisualStudioInstance? s_visualStudioInstance;

    public static int Main(string[] args)
    {
        Helpers.PrintContext();

        s_visualStudioInstance ??= MSBuildLocator.RegisterDefaults();

        return Run(args);
    }

    private static int Run(string[] args)
    {
        var projectOrSolutionArgument = new Argument<FileInfo>(
            name: "projectOrSolution",
            description: "Project or solution to restore.");

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
        readCommand.AddArgument(projectOrSolutionArgument);
        rootCommand.AddCommand(readCommand);

        readCommand.SetHandler(RestoreSubset.Execute, projectOrSolutionArgument, rootDirectoryOption, outputDirectoryOption);

        var commandLineBuilder = new CommandLineBuilder(rootCommand);
        commandLineBuilder.UseDefaults();
        commandLineBuilder.UseExceptionHandler((Exception exception, InvocationContext context) =>
        {
            (int exitCode, string message) = exception switch
            {
                BaseException baseException => (baseException.ExitCode, "ERROR: " + baseException.Message),
                _ => (255, exception.ToString()),
            };

            context.ExitCode = exitCode;
            Console.Error.WriteLine(message);
        });
        var parser = commandLineBuilder.Build();
        return parser.Invoke(args);
    }
}
