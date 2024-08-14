using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernelExperiments.plugins.Python;

[Description("Allow creation and execution of python script")]
internal class PythonExecutor
{
    private readonly PythonExecutorConfiguration _configuration;
    private readonly ILogger<PythonExecutor> _logger;

    public PythonExecutor(PythonExecutorConfiguration configuration, ILogger<PythonExecutor> logger = null)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [KernelFunction("execute")]
    [Description("Execute a python script and return what is printed with the print instruction")]
    [return: Description("All output of print statements.")]
    public async Task<string> ExecuteScript([Description("Complete python script to execute")] string pythonScript)
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempFolder);

        var scriptPath = Path.Combine(tempFolder, "script.py");
        File.WriteAllText(scriptPath, pythonScript);

        _logger.LogTrace("Executing python script in folder {folder}: {script}", tempFolder, pythonScript);

        using (var process = new Process())
        {
            process.StartInfo.FileName = _configuration.PythonLocation;
            process.StartInfo.Arguments = scriptPath;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            //Create a cancellation token for 30 seconds
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            await process.WaitForExitAsync(cancellationToken: cts.Token).ConfigureAwait(false);

            //now that the process exited I can read the output tasks
            string output = await outputTask.ConfigureAwait(false);
            string error = await errorTask.ConfigureAwait(false);

            //need to check if the exit code is ok.
            if (process.ExitCode != 0)
            {
                string exceptionCode = $"Error: {error}";
                throw new Exception(exceptionCode);
            }

            _logger.LogTrace("Python script executed with output: {output}", output);
            return output;
        }
    }
}

public class PythonExecutorConfiguration
{
    /// <summary>
    /// Location of python script in a virtual environment.
    /// </summary>
    public string PythonLocation { get; set; }
}
