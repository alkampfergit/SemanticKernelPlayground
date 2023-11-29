using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planners;

namespace SemanticKernelExperiments;

class Program
{
    static async Task Main(string[] args)
    {
        //string result = Ex01_CallPluginDirectly();

        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddConsole();
        });

        //await Ex02_DirectSequentialCallToExtractVideo(loggerFactory);
        await Ex03_VideoTimelineWithPlanner(loggerFactory);
    }

    private static async Task Ex03_VideoTimelineWithPlanner(ILoggerFactory loggerFactory)
    {
        IKernel kernel = new KernelBuilder()
            .WithAzureOpenAIChatCompletionService(
                "GPT42",
                "https://alkopenai2.openai.azure.com",
                Environment.GetEnvironmentVariable("AI_KEY")
            )
            // Add a text or chat completion service using either:
            // .WithAzureTextCompletionService()
            // .WithAzureChatCompletionService()
            // .WithOpenAITextCompletionService()
            // .WithOpenAIChatCompletionService()
            .WithLoggerFactory(loggerFactory)
            .Build();
        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "plugins");

        // Import the semantic functions
        var publishingPlugin = kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, "PublishingPlugin");
        var concreteAvPlugin = new SemanticKernelExperiments.AudioVideoPlugin.AudioVideoPlugin();
        var audioVideoPlugin = kernel.ImportFunctions(concreteAvPlugin, "AudioVideoPlugin");

        //Now I have kernel with some plugins, time to create the planner
        var planner = new SequentialPlanner(kernel);
        var ask = "I want to extract a summarized timeline from the video file /Users/gianmariaricci/develop/montaggi/UpdatingSSH.mp4";
        var plan = await planner.CreatePlanAsync(ask);

        //we can dump the plan with the serializer.
        Console.WriteLine("Plan:\n");
        Console.WriteLine(
            JsonSerializer.Serialize(
                plan,
                new JsonSerializerOptions { WriteIndented = true }));

        // Execute the plan
        var result = await kernel.RunAsync(plan);

        Console.WriteLine("Plan results:");
        Console.WriteLine(result.GetValue<string>()!.Trim());
    }

    /// <summary>
    /// Simple example that uses direct sequential call to extract audio from a video
    /// using three step plugin
    /// </summary>
    /// <param name="kernel"></param>
    /// <returns></returns>
    private static async Task Ex02_DirectSequentialCallToExtractVideo(ILoggerFactory loggerFactory)
    {
        IKernel kernel = new KernelBuilder()
      .WithAzureOpenAIChatCompletionService(
          "GPT42",
          "https://alkopenai2.openai.azure.com",
          Environment.GetEnvironmentVariable("AI_KEY")
      )
      // Add a text or chat completion service using either:
      // .WithAzureTextCompletionService()
      // .WithAzureChatCompletionService()
      // .WithOpenAITextCompletionService()
      // .WithOpenAIChatCompletionService()
      .WithLoggerFactory(loggerFactory)
      .Build();
        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "plugins");

        // Import the semantic functions
        var publishingPlugin = kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, "PublishingPlugin");
        var concreteAvPlugin = new SemanticKernelExperiments.AudioVideoPlugin.AudioVideoPlugin();
        var audioVideoPlugin = kernel.ImportFunctions(concreteAvPlugin, "AudioVideoPlugin");

        var result = await kernel.RunAsync(
            "/Users/gianmariaricci/develop/montaggi/UpdatingSSH.mp4",
            audioVideoPlugin["ExtractAudio"],
            audioVideoPlugin["TranscriptTimeline"],
            publishingPlugin["VideoTimelineCreator"]);

        Console.WriteLine(result.GetValue<string>());
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