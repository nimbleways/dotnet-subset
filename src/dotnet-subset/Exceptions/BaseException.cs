namespace Nimbleways.Tools.Subset.Exceptions;

public abstract class BaseException : Exception
{
    protected BaseException(string message) : base(message)
    {
    }

    public abstract int ExitCode { get; }
}
