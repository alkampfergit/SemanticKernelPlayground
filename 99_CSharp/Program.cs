using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletionWithData;
using Microsoft.SemanticKernel.PromptTemplate.Handlebars;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticKernelExperiments;

public static class Program
{
    static async Task Main(string[] args)
    {
        // string result = Ex01_CallPluginDirectly();

        // await Ex02_InvokeLLMDirectly();
        // await Ex02_b_InvokeLLMDirectly();

        //await Ex03_DirectSequentialCallToExtractVideo();
        // await Ex03_b_DirectSequentialCallToExtractVideo();

        await Ex04_Load_function_in_builder();
        Console.ReadLine();
    }

    public static async Task Ex02_InvokeLLMDirectly()
    {
        var builder = CreateBasicKernelBuilder();
        var kernel = builder.Build();
        var result = await kernel.InvokePromptAsync("How are you today");
        Console.WriteLine(result);
    }

    public static async Task Ex02_b_InvokeLLMDirectly()
    {
        var builder = CreateBasicKernelBuilder();
        var kernel = builder.Build();

        var chatPrompt = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Prompts",
            "chat.yaml");
        var promptContent = File.ReadAllText(chatPrompt);
        KernelFunction prompt = kernel.CreateFunctionFromPromptYaml(
            promptContent,
            promptTemplateFactory: new HandlebarsPromptTemplateFactory()
        );

        ChatHistory chatMessages = new();
        chatMessages.AddUserMessage("Hi what is your name?");
        chatMessages.AddAssistantMessage("I am an Assistant ai but you can call me Jarvis");
        chatMessages.AddUserMessage("My name is Gian Maria");
        chatMessages.AddAssistantMessage("Hi Gian Maria how can I help you?");
        chatMessages.AddUserMessage("Tell my name and repeat how can I call you!");
        var result = await kernel.InvokeAsync<string>(
            prompt,
            new KernelArguments()
            {
                ["messages"] = chatMessages
            });
        Console.WriteLine("Result: {0}", result);
    }

    /// <summary>
    /// Simple example that uses direct sequential call to extract audio from a video
    /// using three step plugin. This is updated to the new RC3 syntax to call plugin
    /// function directly
    /// </summary>
    /// <returns></returns>
    private static async Task Ex03_DirectSequentialCallToExtractVideo()
    {
        KernelBuilder kernelBuilder = CreateBasicKernelBuilder();

        var kernel = kernelBuilder.Build();

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "plugins", "PublishingPlugin");

        var publishingPlugin = kernel.ImportPluginFromPromptDirectory(pluginsDirectory, "PublishingPlugin");
        Console.WriteLine("Imported {0} functions from {1}", publishingPlugin.Count(), publishingPlugin.Name);

        var concreteAvPlugin = new SemanticKernelExperiments.AudioVideoPlugin.AudioVideoPlugin();
        var audioVideoPlugin = kernel.ImportPluginFromObject(concreteAvPlugin, "AudioVideoPlugin");
        Console.WriteLine("Imported {0} functions from {1}", audioVideoPlugin.Count(), audioVideoPlugin.Name);

        audioVideoPlugin.TryGetFunction("ExtractAudio", out var extractAudio);
        KernelArguments args = new KernelArguments();
        args["videofile"] = @"C:\temp\ssh.mp4";
        var callresult = await extractAudio.InvokeAsync(kernel, args);
        var audioFile = callresult.GetValue<string>();

        audioVideoPlugin.TryGetFunction("TranscriptTimeline", out var transcriptTimeline);
        args = new KernelArguments();
        args["audioFile"] = audioFile;
        callresult = await transcriptTimeline.InvokeAsync(kernel, args);
        var transcript = callresult.GetValue<string>();

        //ok now we must call the last function, summarization
        publishingPlugin.TryGetFunction("VideoTimelineCreator", out var videoTimelineCreator);
        args = new KernelArguments();
        args["transcript"] = transcript;
        callresult = await videoTimelineCreator.InvokeAsync(kernel, args);
        var timeline = callresult.GetValue<string>();
        Console.WriteLine(timeline);
    }

    private static async Task Ex03_b_DirectSequentialCallToExtractVideo()
    {
        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "plugins", "PublishingPlugin");
        KernelBuilder kernelBuilder = CreateBasicKernelBuilder();
        kernelBuilder
           .Plugins
               .AddFromType<AudioVideoPlugin.AudioVideoPlugin>("AudioVideoPlugin")
               .AddFromPromptDirectory(pluginsDirectory);
        var kernel = kernelBuilder.Build();

        KernelArguments args = new KernelArguments();
        args["videofile"] = @"C:\temp\ssh.mp4";
        var result = await kernel.InvokeAsync("AudioVideoPlugin", "ExtractAudio", args);
        var audioFile = result.GetValue<string>();

        args["audioFile"] = audioFile;
        result = await kernel.InvokeAsync("AudioVideoPlugin", "TranscriptTimeline", args);
        var fullTranscript = result.GetValue<string>();

        args["transcript"] = fullTranscript;
        result = await kernel.InvokeAsync("PublishingPlugin", "VideoTimelineCreator", args);
        var timeline = result.GetValue<string>();
        Console.WriteLine(timeline);
    }

    private static async Task Ex04_Load_function_in_builder()
    {
        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "plugins", "PublishingPlugin");
        KernelBuilder kernelBuilder = CreateBasicKernelBuilder();
        kernelBuilder
            .Plugins
                .AddFromType<AudioVideoPlugin.AudioVideoPlugin>("AudioVideoPlugin")
                .AddFromPromptDirectory(pluginsDirectory);
        var kernel = kernelBuilder.Build();

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionCallBehavior = FunctionCallBehavior.AutoInvokeKernelFunctions
        };

        var chatPrompt = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Prompts", "chat.yaml");
        var promptContent = File.ReadAllText(chatPrompt);
        KernelFunction prompt = kernel.CreateFunctionFromPromptYaml(
            promptContent,
            promptTemplateFactory: new HandlebarsPromptTemplateFactory()
        );

        ChatHistory chatMessages = new();
        chatMessages.AddUserMessage("I want to extract audio from video file C:\\temp\\ssh.mp4");
        var result = await kernel.InvokeAsync<string>(
            prompt,
            arguments: new(openAIPromptExecutionSettings) {
                { "messages", chatMessages }
            });

        Console.WriteLine("Result: {0}", result);

        chatMessages = new();
        chatMessages.AddUserMessage("I want to extract summarized timeline from video file C:\\temp\\ssh.mp4");
        var complexResult = await kernel.InvokeAsync(
            prompt,
            arguments: new(openAIPromptExecutionSettings) {
                { "messages", chatMessages }
            });

        Console.WriteLine("Result: {0}", complexResult.GetValue<string>());
    }

    private static KernelBuilder CreateBasicKernelBuilder()
    {
        var kernelBuilder = new KernelBuilder();
        kernelBuilder.Services.AddLogging(l => l
            .SetMinimumLevel(LogLevel.Trace)
            .AddConsole()
            .AddDebug()
        );

        kernelBuilder.Services.AddAzureOpenAIChatCompletion(
            "GPT4t",
            "gpt-4", //Model Name,
            Dotenv.Get("OPENAI_API_BASE"),
            Dotenv.Get("OPENAI_API_KEY"));

        return kernelBuilder;
    }

    /// <summary>
    /// Sample example to show how to call direclty the plugin audio video
    /// to extract audio then call python function with a wrapper to
    /// call the python script that uses openai whisper
    /// </summary>
    /// <returns></returns>
    private static string Ex01_CallPluginDirectly()
    {
        var av = new AudioVideoPlugin.AudioVideoPlugin();
        av.ExtractAudio("/Users/gianmariaricci/develop/montaggi/UpdatingSSH.mp4");

        var python = new PythonWrapper("/Users/gianmariaricci/develop/github/SemanticKernelPlayground/skernel/bin/python3");
        var script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python", "transcript_timeline.py");
        var result = python.Execute(script, "/Users/gianmariaricci/develop/montaggi/UpdatingSSH.wav");
        Console.WriteLine(result);
        return result;
    }
}