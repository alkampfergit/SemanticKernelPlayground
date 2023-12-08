using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.PromptTemplate.Handlebars;

namespace SemanticKernelExperiments;

class Program
{
    static async Task Main(string[] args)
    {
        // string result = Ex01_CallPluginDirectly();
        // Console.WriteLine(result);

        // await Ex02_InvokeLLMDirectly();
        //await Ex03_DirectSequentialCallToExtractVideo();
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

    // private static async Task Ex03_VideoTimelineWithPlanner(ILoggerFactory loggerFactory)
    // {
    //     var kernelBuilder = new KernelBuilder();
    //     var kernel = kernelBuilder.WithAzureOpenAIChatCompletion(
    //         "GPT42",
    //         "gpt-4", //Model Name,
    //          "https://openaiswedenalk.openai.azure.com/",
    //          Environment.GetEnvironmentVariable("AI_KEY"))
    //         .WithLoggerFactory(loggerFactory)
    //         .Build();

    //     var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "plugins");

    //     // Import the semantic functions
    //     var publishingPlugin = kernel.ImportPluginFromPromptDirectory(pluginsDirectory, "PublishingPlugin");
    //     var concreteAvPlugin = new SemanticKernelExperiments.AudioVideoPlugin.AudioVideoPlugin();
    //     var audioVideoPlugin = kernel.ImportPluginFromObject(concreteAvPlugin, "AudioVideoPlugin");

    //     //Now I have kernel with some plugins, time to create the planner
    //     var planner = new SequentialPlanner();
    //     var ask = "I want to extract a summarized timeline from the video file /Users/gianmariaricci/develop/montaggi/UpdatingSSH.mp4";
    //     var plan = await planner.CreatePlanAsync(ask);

    //     //we can dump the plan with the serializer.
    //     Console.WriteLine("Plan:\n");
    //     Console.WriteLine(
    //         JsonSerializer.Serialize(
    //             plan,
    //             new JsonSerializerOptions { WriteIndented = true }));

    //     // Execute the plan
    //     var result = await kernel.RunAsync(plan);

    //     Console.WriteLine("Plan results:");
    //     Console.WriteLine(result.GetValue<string>()!.Trim());
    // }

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

        // Import the semantic functions
        // var finfo = Path.Combine(pluginsDirectory, "PublishingPlugin", "VideoTimelineCreator", "config.json");
        // var json = File.ReadAllText(finfo);

        var publishingPlugin = kernel.ImportPluginFromPromptDirectory(pluginsDirectory, "PublishingPlugin");
        Console.WriteLine("Imported {0} functions from {1}", publishingPlugin.Count(), publishingPlugin.Name);
        var concreteAvPlugin = new SemanticKernelExperiments.AudioVideoPlugin.AudioVideoPlugin();
        var audioVideoPlugin = kernel.ImportPluginFromObject(concreteAvPlugin, "AudioVideoPlugin");
        Console.WriteLine("Imported {0} functions from {1}", audioVideoPlugin.Count(), audioVideoPlugin.Name);

        audioVideoPlugin.TryGetFunction("ExtractAudio", out var extractAudio);
        KernelArguments args = new KernelArguments();
        args["videofile"] = "/Users/gianmariaricci/develop/montaggi/UpdatingSSH.mp4";
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

    private static async Task Ex04_Load_function_in_builder()
    {
        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "plugins", "PublishingPlugin");
        var chatPrompt = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Prompts", "chat.yaml");
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

        var promptContent = File.ReadAllText(chatPrompt);
        KernelFunction prompt = kernel.CreateFunctionFromPromptYaml(
            promptContent,
            promptTemplateFactory: new HandlebarsPromptTemplateFactory()
        );

        ChatHistory chatMessages = new();
        chatMessages.AddUserMessage("I want to extract audio from video file /Users/gianmariaricci/develop/montaggi/UpdatingSSH.mp4");
        var result = await kernel.InvokeAsync<string>(
            prompt,
            arguments: new(openAIPromptExecutionSettings) {
                { "messages", chatMessages }
            });
        
        Console.WriteLine("Result: {0}", result);


        // chatMessages = new();
        // chatMessages.AddUserMessage("I want to extract full transcript from video file /Users/gianmariaricci/develop/montaggi/UpdatingSSH.mp4");
        // result = await kernel.InvokeAsync<string>(
        //     prompt,
        //     arguments: new(openAIPromptExecutionSettings) {
        //         { "messages", chatMessages }
        //     });
        
        // Console.WriteLine("Result: {0}", result);

        
        chatMessages = new();
        chatMessages.AddUserMessage("I want to extract summarized timeline from video file /Users/gianmariaricci/develop/montaggi/UpdatingSSH.mp4");
        result = await kernel.InvokeAsync<string>(
            prompt,
            arguments: new(openAIPromptExecutionSettings) {
                { "messages", chatMessages }
            });
        
        Console.WriteLine("Result: {0}", result);
    }

    private static KernelBuilder CreateBasicKernelBuilder()
    {
        var kernelBuilder = new KernelBuilder();
        kernelBuilder.Services.AddLogging(l => l.AddConsole().SetMinimumLevel(LogLevel.Trace));
        kernelBuilder.Services.AddAzureOpenAIChatCompletion(
            "GPT42",
            "gpt-4", //Model Name,
             "https://alkopenai2.openai.azure.com/",
             Environment.GetEnvironmentVariable("AI_KEY"));
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