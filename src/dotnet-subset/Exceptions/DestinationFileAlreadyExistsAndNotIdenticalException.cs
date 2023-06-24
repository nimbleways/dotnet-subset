namespace Nimbleways.Tools.Subset.Exceptions;

public class DestinationFileAlreadyExistsAndNotIdenticalException : BaseException
{
    public const int EXIT_CODE = 30;

    public DestinationFileAlreadyExistsAndNotIdenticalException(FileInfo sourceFile, FileInfo destinationFile)
        : base($"Cannot copy '{sourceFile.FullName}' to '{destinationFile.FullName}': destination file already exist but have different content from source file")
    {

    }

    public override int ExitCode { get; } = EXIT_CODE;
}
