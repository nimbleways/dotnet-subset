// Inspired from https://github.com/dotnet/aspnetcore/blob/69254997/src/Shared/Process/ProcessEx.cs

using System.Diagnostics;
using System.Text;

using Nimbleways.Tools.Subset.Utils.Processes.Exceptions;

namespace Nimbleways.Tools.Subset.Utils.Processes;

internal sealed class ProcessRunner : IDisposable
{
    private readonly Process _process;
    private readonly StringBuilder _outputCapture;
    private readonly object _pipeCaptureLock = new();
    private bool _disposed;

    public ProcessRunner(DirectoryInfo workingDirectory, string command, string args)
    {
        _process = new()
        {
            StartInfo = new ProcessStartInfo(command, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory.FullName
            },
            EnableRaisingEvents = true,
        };
        _process.OutputDataReceived += OnDataReceived;
        _process.ErrorDataReceived += OnDataReceived;

        _outputCapture = new StringBuilder();
    }

    public ProcessResult Run(TimeSpan timeout)
    {
        if (!_process.Start())
        {
            return new ProcessFailureResult(new ProcessStartException(_process));
        }

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        if (!_process.WaitForExit((int)timeout.TotalMilliseconds))
        {
            return new ProcessFailureResult(new ProcessTimeoutException(_process, timeout));
        }

        return new ProcessExitedResult(_process.ExitCode, _outputCapture.ToString());
    }

    private void OnDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
        {
            return;
        }

        lock (_pipeCaptureLock)
        {
            _outputCapture.AppendLine(e.Data);
            if (!_disposed)
            {
                Console.WriteLine(e.Data);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!_process.HasExited)
        {
            _process.KillTree(TimeSpan.FromSeconds(30));
        }

        _process.CancelOutputRead();
        _process.CancelErrorRead();

        _process.ErrorDataReceived -= OnDataReceived;
        _process.OutputDataReceived -= OnDataReceived;
        _process.Dispose();
    }
}
