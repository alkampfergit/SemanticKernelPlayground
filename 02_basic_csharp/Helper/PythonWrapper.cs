namespace _02_basic_csharp;
using System.Diagnostics;
using System.IO;

public class PythonWrapper
{
    private readonly string python3Location;

    public PythonWrapper(string python3location)
    {
        python3Location = python3location;
    }
    public string Execute(string scriptPath, string arguments = "")
    {
        if (!File.Exists(python3Location))
        {
            throw new FileNotFoundException($"Python3 not found at {python3Location}");
        }

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Script not found at {scriptPath}");
        }
        
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = python3Location;
        start.Arguments = $"{scriptPath} {arguments}"; // Add the arguments to the command line
        
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;

        using Process process = Process.Start(start);

        using System.IO.StreamReader reader = process.StandardOutput;

        string result = reader.ReadToEnd();
        process.WaitForExit();
        return result;
    }
}
