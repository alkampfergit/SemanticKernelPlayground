using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticKernel.Orchestration.Assistants;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticKernelExperiments.AudioVideoPlugin;

public class AudioVideoAssistant : BaseAssistant
{
    public const string AudioVideoAssistantAgentName = "AudioVideoAssistant";
    public AudioVideoAssistant() : base(AudioVideoAssistantAgentName)
    {
        RegisterFunctionDelegate(
            "ExtractAudio",
            KernelFunctionFactory.CreateFromMethod(ExtractAudio),
            async (args) => await ExtractAudio(args["videofile"].ToString()!));

        RegisterFunctionDelegate(
            "Transcribe",
            KernelFunctionFactory.CreateFromMethod(Transcribe),
            async (args) => await Transcribe(args["audiofile"].ToString()!));
    }

    [Description("extract audio in wav format from an mp4 file")]
    private async Task<AssistantResponse> ExtractAudio([Description("Full path to the mp4 file")] string videofile)
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
        return new AssistantResponse(audioPath, new State("ExtractAudio", videofile, audioPath));
    }

    [Description("Transcribe text from audio file")]
    private async Task<AssistantResponse> Transcribe([Description("Full path to the audio file")] string audiofile)
    {
        Console.WriteLine($"Transcribing text from audio: {audiofile}");

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            string command = $@"whisper ""{audiofile}"" --task transcribe --output_format txt --output_dir ""{tempDir}"" --model tiny";
            using (var process = new Process())
            {
                process.StartInfo.FileName = "whisper";
                process.StartInfo.Arguments = $"{command}";
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.RedirectStandardError = false;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = false;

                process.Start();
                await process.WaitForExitAsync();
            }

            //todo: HAndle errors
            var textFile = Directory.GetFiles(tempDir, "*.txt").FirstOrDefault();
            if (textFile == null)
            {
                return "Unable to transcript the audio";
            }

            string transcription = File.ReadAllText(textFile);
            SetLocalProperty("transcription", transcription);
            SetGlobalProperty("transcription", transcription);
            return new AssistantResponse("transcription done", new State( "Transcribe", audiofile, ""));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private record State(string Operation, string InputFile, string OutputFile);

    public override void AddResultToPrompt(ChatHistory chatHistory, AssistantResponse agentOperationResult)
    {
        var realState = (State)agentOperationResult.State!;
        if (realState.Operation == "ExtractAudio")
        {
            chatHistory.AddAssistantMessage($"Audio extracted from video {realState.InputFile} extracted to file {realState.OutputFile}");
        }
        else if (realState.Operation == "Transcribe")
        {
            chatHistory.AddAssistantMessage($"agent {AudioVideoAssistantAgentName} has trancription of file {realState.InputFile}, in Transcription property");
        }
        else
        {
            //Error 
            throw new Exception("Unknown function");
        }
    }

    public override string GetFact(AssistantResponse agentOperationResult)
    {
        var realState = (State)agentOperationResult.State!;
        if (realState.Operation == "ExtractAudio")
        {
            return $"Audio was extracted from video {realState.InputFile} to file {realState.OutputFile}";
        }

        if (realState.Operation == "Transcribe")
        {
            return $"agent {AudioVideoAssistantAgentName} has trancription of file {realState.InputFile}, in Transcription property";
        }

        //Error 
        throw new Exception("Unknown function");
    }
}
