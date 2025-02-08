using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticKernel.Orchestration.Assistants;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SemanticKernelExperiments.AudioVideoPlugin;
public class AudioVideoAssistant : BaseAssistant
{
    public AudioVideoAssistant() : base("AudioVideoAssistant")
    {
        RegisterFunctionDelegate(
            "ExtractAudio",
            KernelFunctionFactory.CreateFromMethod(ExtractAudio),
            async (args) => await ExtractAudio(args["videofile"].ToString()!));
    }

    [Description("extract audio in wav format from an mp4 file")]
    private async Task<string> ExtractAudio([Description("Full path to the mp4 file")] string videofile)
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
            await process.WaitForExitAsync();
        }

        // Now ffmpeg has created the audio file, return the path to it
        return audioPath;
    }

    public override void AddStateToPrompt(ChatHistory chatHistory)
    {
        foreach (var state in base._stateList)
        {
            if (state.FunctionName == "ExtractAudio")
            {
                chatHistory.AddAssistantMessage($"Audio extracted from video {state.Arguments["videofile"]} extracted to file {state.Result}");
            }
            else 
            {
                //Error 
                throw new Exception("Unknown function");
            }
        }
    }

    public override List<string> GetFacts()
    {
        List<string> facts = new();
        foreach (var state in base._stateList)
        {
            if (state.FunctionName == "ExtractAudio")
            {
                facts.Add($"Audio was extracted from video {state.Arguments["videofile"]} to file {state.Result}");
            }
            else
            {
                //Error 
                throw new Exception("Unknown function");
            }
        }

        return facts;
    }

    //[KernelFunction, Description("Transcript audio from a wav file to a timeline extracting a transcript")]
    //[return: Description("Transcript of an audio file with time markers")]
    //public string TranscriptTimeline([Description("Full path to the wav file")] string audioFile)
    //{
    //    var python = new PythonWrapper(@"C:\develop\github\SemanticKernelPlayground\skernel\Scripts\python.exe");
    //    var script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python", "transcript_timeline.py");
    //    var result = python.Execute(script, audioFile);
    //    return result;
    //}
}
