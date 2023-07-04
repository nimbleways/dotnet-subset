using System.Diagnostics;

namespace Nimbleways.Tools.Subset.Utils.Processes.Exceptions;

public class ProcessStartException : ProcessBaseException
{
    public ProcessStartException(Process process)
        : base($"Could not start process: {process.StartInfo.FileName} {process.StartInfo.Arguments}") { }
}
