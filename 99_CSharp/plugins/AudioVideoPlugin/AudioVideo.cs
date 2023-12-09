using Microsoft.SemanticKernel;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace SemanticKernelExperiments.AudioVideoPlugin;
public class AudioVideoPlugin
{
    [KernelFunction, Description("extract audio in wav format from an mp4 file")]
    public string ExtractAudio([Description("Full path to the mp4 file")] string videofile)
    {
        Console.WriteLine($"Extracting audio file from video {videofile}");
        // First of all, change the extension of the video file to create the output path
        string audioPath = videofile.Replace(".mp4", ".wav", StringComparison.OrdinalIgnoreCase);

        // If the audio file exists, delete it, maybe it is an old version
        if (File.Exists(audioPath))
        {
            File.Delete(audioPath);
        }

        string command = $"-i {videofile} -vn -acodec pcm_s16le -ar 44100 -ac 2 {audioPath}";
        using (var process = new Process())
        {
            process.StartInfo.FileName = "ffmpeg";
            process.StartInfo.Arguments = $"{command}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            process.WaitForExit();
        }

        // Now ffmpeg has created the audio file, return the path to it
        return audioPath;
    }

    [KernelFunction, Description("Transcript audio from a wav file to a timeline extracting a transcript")]
    public string TranscriptTimeline([Description("Full path to the wav file")] string audioFile)
    {
        var python = new PythonWrapper(@"C:\develop\github\SemanticKernelPlayground\skernel\Scripts\python.exe");
        var script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python", "transcript_timeline.py");
        var result = python.Execute(script, audioFile);
        return result;
    }
}
