using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernelExperiments.Helper;

internal struct ProcessResult
{
    public string Output { get; set; }
    public string Error { get; set; }
    public int ExitCode { get; set; }
}

internal class ProcessHelper
{
    private readonly string _currentDirectory;

    public ProcessHelper(string currentDirectory)
    {
        _currentDirectory = currentDirectory;
    }

    public async Task<ProcessResult> InvokeProcessAsync(string fileName, string arguments, int timeoutSeconds = 30)
    {
        using (var process = new Process())
        {
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = _currentDirectory;

            process.Start();

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            await process.WaitForExitAsync(cancellationToken: cts.Token).ConfigureAwait(false);

            string output = await outputTask.ConfigureAwait(false);
            string error = await errorTask.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                string exceptionCode = $"Error: {error}";
                throw new Exception(exceptionCode);
            }

            return new ProcessResult
            {
                Output = output,
                Error = error,
                ExitCode = process.ExitCode
            };
        }
    }
}