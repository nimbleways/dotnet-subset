using System.Diagnostics;

namespace Nimbleways.Tools.Subset.Utils.Processes.Exceptions;

public class ProcessTimeoutException : ProcessBaseException
{
    public ProcessTimeoutException(Process process, TimeSpan timeout)
        : base($"Process proc {process.ProcessName} {process.StartInfo.Arguments} timed out after {timeout}.") { }
}
