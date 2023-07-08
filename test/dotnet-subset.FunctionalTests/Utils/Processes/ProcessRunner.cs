// Inspired from https://github.com/dotnet/aspnetcore/blob/69254997/src/Shared/Process/ProcessEx.cs

using System.Diagnostics;
using System.Text;

using Nimbleways.Tools.Subset.Utils.Processes.Exceptions;

namespace Nimbleways.Tools.Subset.Utils.Processes;

internal sealed class ProcessRunner : IDisposable
{
    private readonly Process _process;
    private readonly StringBuilder _outputCapture;
    private readonly ManualResetEventSlim _stdoutHandle;
    private readonly ManualResetEventSlim _stderrHandle;
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
        _process.OutputDataReceived += OnOutputDataReceived;
        _process.ErrorDataReceived += OnErrorDataReceived;
        _stdoutHandle = new ManualResetEventSlim(false);
        _stderrHandle = new ManualResetEventSlim(false);

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
        _stdoutHandle.Wait();
        _stderrHandle.Wait();
        return new ProcessExitedResult(_process.ExitCode, _outputCapture.ToString());
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
        {
            _stdoutHandle.Set();
            return;
        }

        HandleData(e.Data);
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
        {
            _stderrHandle.Set();
            return;
        }

        HandleData(e.Data);
    }

    private void HandleData(string data)
    {
        lock (_pipeCaptureLock)
        {
            _outputCapture.AppendLine(data);
            if (!_disposed)
            {
                Console.WriteLine(data);
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

        _process.ErrorDataReceived -= OnOutputDataReceived;
        _process.OutputDataReceived -= OnOutputDataReceived;
        _stdoutHandle.Dispose();
        _stderrHandle.Dispose();
        _process.Dispose();
    }
}
