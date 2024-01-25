using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning.Handlebars;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using SemanticKernelExperiments.Helper;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticKernelExperiments;

public static class Program
{
    private static DumpLoggingProvider _loggingProvider = new DumpLoggingProvider();
    static async Task Main(string[] args)
    {
        //string result = Ex01_CallPluginDirectly();

        //await Ex02_InvokeLLMDirectly();
        //await Ex02_b_InvokeLLMDirectly();

        // await Ex03_DirectSequentialCallToExtractVideo();
        // await Ex03_b_DirectSequentialCallToExtractVideo();

        //await Ex04_Load_function_in_builder();
        await Ex05_basic_planner();
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

        Console.WriteLine("\n\nProcess followed to answer the question:\n");

        DumpTextSection("RAW FUNCTION CALLS");
        var llmCalls = _loggingProvider.GetLLMCalls();
        foreach (var llmCall in llmCalls)
        {
            Console.WriteLine($"Function {llmCall.ResponseFunctionCall} with arguments {llmCall.ResponseFunctionCallParameters}");
        }

        DumpTextSection("FULL INFORMATION DUMP");
        foreach (var llmCall in llmCalls)
        {
            Console.WriteLine(llmCall.Dump());
        }
    }

    private static async Task Ex05_basic_planner()
    {
        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "plugins", "PublishingPlugin");
        var kernelBuilder = CreateBasicKernelBuilder();
        kernelBuilder
            .Plugins
                .AddFromType<AudioVideoPlugin.AudioVideoPlugin>("AudioVideoPlugin")
                .AddFromPromptDirectory(pluginsDirectory);
        var kernel = kernelBuilder.Build();

#pragma warning disable SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var planner = new HandlebarsPlanner();

        var question = "I want to extract summarized timeline from video file C:\\temp\\ssh.mp4";
        var plan = await planner.CreatePlanAsync(kernel, question);
        var textPlan = plan.ToString();
        Console.WriteLine(textPlan);

        var llmCalls = _loggingProvider.GetLLMCalls();
        var callToExecuteThePlan = llmCalls.Count();
        Console.WriteLine("Number of function calls to generate the plan: {0}", callToExecuteThePlan);

        var planCall = llmCalls.Single();
        //now we can invoke the plan
        var result = await plan.InvokeAsync(kernel);

        llmCalls = _loggingProvider.GetLLMCalls();
        Console.WriteLine("Num of function calls to execute the plan: {0}", llmCalls.Count() - callToExecuteThePlan);

        Console.WriteLine("Result: {0}", result);

#pragma warning restore SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        DumpTextSection("RAW FUNCTION CALLS");
        foreach (var llmCall in llmCalls)
        {
            Console.WriteLine($"Function {llmCall.ResponseFunctionCall} with arguments {llmCall.ResponseFunctionCallParameters}");
        }

        DumpTextSection("FULL INFORMATION DUMP");
        foreach (var llmCall in llmCalls)
        {
            Console.WriteLine(llmCall.Dump());
        }
    }

    private static void DumpTextSection(string text)
    {
        int totalWidth = 80;
        int padding = (totalWidth - text.Length) / 2;
        string centeredText = text.PadLeft(padding + text.Length).PadRight(totalWidth);
        Console.WriteLine(new string('-', totalWidth));
        Console.WriteLine(centeredText);
        Console.WriteLine(new string('-', totalWidth));
    }

    private static IKernelBuilder CreateBasicKernelBuilder()
    {
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddLogging(l => l
            .SetMinimumLevel(LogLevel.Trace)
            .AddConsole()
            .AddDebug()
            .AddProvider(_loggingProvider)
        );

        kernelBuilder.Services.ConfigureHttpClientDefaults(c => c
            .AddLogger(s => _loggingProvider.CreateHttpRequestBodyLogger(s.GetRequiredService<ILogger<DumpLoggingProvider>>())));

        kernelBuilder.Services.AddAzureOpenAIChatCompletion(
            "GPT42", //"GPT35_2",//"GPT42",
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