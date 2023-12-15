using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticKernelExperiments;

public static class Program
{
    static async Task Main(string[] args)
    {
        //string result = Ex01_CallPluginDirectly();

        //await Ex02_InvokeLLMDirectly();
        //await Ex02_b_InvokeLLMDirectly();

        // await Ex03_DirectSequentialCallToExtractVideo();
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
        var kernelBuilder = CreateBasicKernelBuilder();

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
        var kernelBuilder = CreateBasicKernelBuilder();
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
        var kernelBuilder = CreateBasicKernelBuilder();
        kernelBuilder
            .Plugins
                .AddFromType<AudioVideoPlugin.AudioVideoPlugin>("AudioVideoPlugin")
                .AddFromPromptDirectory(pluginsDirectory);
        var kernel = kernelBuilder.Build();

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        };

        var chatPrompt = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Prompts", "chat.yaml");
        var promptContent = File.ReadAllText(chatPrompt);
        KernelFunction prompt = kernel.CreateFunctionFromPromptYaml(
            promptContent,
            promptTemplateFactory: new HandlebarsPromptTemplateFactory()
        );

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        //ChatHistory chatMessages = new();
        //chatMessages.AddUserMessage("I want to extract audio from video file C:\\temp\\ssh.mp4");
        //var result = await chatCompletionService.GetChatMessageContentsAsync(
        //      chatMessages,
        //      executionSettings: openAIPromptExecutionSettings,
        //      kernel: kernel);

        //Console.WriteLine("Result: {0}", result[result.Count - 1].Content);

        //chatMessages = new();
        //chatMessages.AddUserMessage("I want to extract full timeline from video file C:\\temp\\ssh.mp4");
        //result = await chatCompletionService.GetChatMessageContentsAsync(
        //      chatMessages,
        //      executionSettings: openAIPromptExecutionSettings,
        //      kernel: kernel);

        //Console.WriteLine("Result: {0}", result[result.Count - 1].Content);

        ChatHistory chatMessages = new();
        chatMessages.AddUserMessage("I want to extract summarized timeline from video file C:\\temp\\ssh.mp4");
        var result = await chatCompletionService.GetChatMessageContentsAsync(
              chatMessages,
              executionSettings: openAIPromptExecutionSettings,
              kernel: kernel);

        Console.WriteLine("Result: {0}", result[result.Count - 1].Content);
    }

    private static IKernelBuilder CreateBasicKernelBuilder()
    {
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddLogging(l => l
            .SetMinimumLevel(LogLevel.Trace)
            .AddConsole()
            .AddDebug()
        );

        kernelBuilder.Services.AddAzureOpenAIChatCompletion(
            "GPT4t",
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
        av.ExtractAudio(@"C:\temp\ssh.mp4");

        var python = new PythonWrapper(@"C:\develop\github\SemanticKernelPlayground\skernel\Scripts\python.exe");
        var script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python", "transcript_timeline.py");
        var result = python.Execute(script, @"C:\temp\ssh.wav");
        Console.WriteLine(result);
        return result;
    }
}