using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

using Nimbleways.Tools.Subset.Exceptions;

namespace Nimbleways.Tools.Subset;

internal static class CommandLineParser
{
    public static Parser GetCommandLineParser(Action initialize)
    {
        var rootCommand = new RootCommand(".NET Tool to copy a subset of files from a repository to a directory.");
        rootCommand.AddCommand(GetRestoreCommand());
        return GetCommandLineBuilder(rootCommand, initialize).Build();
    }

    private static CommandLineBuilder GetCommandLineBuilder(RootCommand rootCommand, Action initialize)
    {
        var commandLineBuilder = new CommandLineBuilder(rootCommand);
        commandLineBuilder.UseDefaults();
        commandLineBuilder.AddMiddleware(PrintApplicationAndRuntimeVersions);
        commandLineBuilder.AddMiddleware(_ => initialize());
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
        return commandLineBuilder;
    }

    private static void PrintApplicationAndRuntimeVersions(InvocationContext context)
    {
        if (!context.ParseResult.HasOption(NoLogoOption))
        {
            Helpers.PrintApplicationAndRuntimeVersions();
        }
    }

    private static readonly Option<bool> NoLogoOption = new("--nologo", "Do not display the startup banner.");

    private static Command GetRestoreCommand()
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

        var restoreCommand = new Command("restore", "Create a subset for the restore operation.")
        {
            rootDirectoryOption,
            outputDirectoryOption,
            NoLogoOption,
        };
        restoreCommand.AddArgument(projectOrSolutionArgument);
        restoreCommand.SetHandler(RestoreSubset.Execute, projectOrSolutionArgument, rootDirectoryOption, outputDirectoryOption);
        return restoreCommand;
    }
}
