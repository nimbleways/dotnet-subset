using Nimbleways.Tools.Subset.Utils.Processes.Exceptions;

namespace Nimbleways.Tools.Subset.Utils.Processes;

internal abstract record ProcessResult;

internal sealed record ProcessExitedResult(int ExitCode, string Output) : ProcessResult;

internal sealed record ProcessFailureResult(ProcessBaseException Exception) : ProcessResult;

