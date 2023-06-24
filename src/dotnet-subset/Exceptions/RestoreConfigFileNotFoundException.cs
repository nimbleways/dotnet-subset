namespace Nimbleways.Tools.Subset.Exceptions;

public class RestoreConfigFileNotFoundException : BaseException
{
    public const int EXIT_CODE = 20;

    public RestoreConfigFileNotFoundException(string projectPath, string restoreConfigFile)
        : base(GetMessage(projectPath, restoreConfigFile))
    {
    }

    private static string GetMessage(string projectPath, string restoreConfigFile)
    {
        string errorMessage = $"cannot find the file '{restoreConfigFile}' defined in the property 'RestoreConfigFile' of the project '{projectPath}'";
        errorMessage += $"{Environment.NewLine}If the path is relative, it is resolved by NuGet against the current working directly, not the project directory";
        return errorMessage;
    }
    public override int ExitCode { get; } = EXIT_CODE;
}
