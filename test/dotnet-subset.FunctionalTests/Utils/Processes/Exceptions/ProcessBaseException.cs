namespace Nimbleways.Tools.Subset.Utils.Processes.Exceptions;

public abstract class ProcessBaseException : Exception
{
    protected ProcessBaseException(string message)
        : base(message) { }
}
