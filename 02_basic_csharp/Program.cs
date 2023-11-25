using System;
using System.IO;

namespace _02_basic_csharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var av = new AudioVideoPlugin();
            av.ExtractAudio("/Users/gianmariaricci/develop/montaggi/UpdatingSSH.mp4");

            var python = new PythonWrapper("/Users/gianmariaricci/develop/github/SemanticKernelPlayground/skernel/bin/python3");
            var script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python", "transcript_timeline.py");
            var result = python.Execute(script, "/Users/gianmariaricci/develop/montaggi/UpdatingSSH.wav");
            Console.WriteLine(result);
        }
    }
}