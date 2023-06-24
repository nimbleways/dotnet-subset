namespace Nimbleways.Tools.Subset.Exceptions;

public class InvalidRootDirectoryException : BaseException
{
    public const int EXIT_CODE = 10;

    public InvalidRootDirectoryException(FileInfo projectOrSolution, DirectoryInfo rootFolder)
        : base($"The root folder '{rootFolder.FullName}' is not a parent of the project or solution '{projectOrSolution.FullName}'")
    {
    }

    public override int ExitCode { get; } = EXIT_CODE;
}
