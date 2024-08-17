using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning.Handlebars;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using SemanticKernelExperiments.Helper;
using SemanticKernelExperiments.Helper.LogHelpers;
using SemanticKernelExperiments.plugins.dotnet;
using SemanticKernelExperiments.plugins.FileSystem;
using SemanticKernelExperiments.plugins.Math;
using SemanticKernelExperiments.plugins.Python;
using SemanticKernelExperiments.plugins.Search;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SemanticKernelExperiments.plugins.FileSystem.FileSystemPlugin;

namespace SemanticKernelExperiments;

public static class Program
{
    private static DumpLoggingProvider _loggingProvider = new DumpLoggingProvider();

    static async Task Main(string[] args)
    {
        //string result = Ex01_CallPluginDirectly();

        //await Ex02_InvokeLLMDirectly();
        //await Ex02a_InvokeOpenaiClient();
        //await Ex02_b_InvokeLLMDirectly();
        //await Ex02_c_InvokeLLMDirectly();
        //await Ex02_c_InvokeLLWithTools();
        //await Ex02_d_InvokeLLMDirectly_handlebar();

        //await Ex03_DirectSequentialCallToExtractVideo();
        //await Ex03_b_DirectSequentialCallToExtractVideo();

        //await Ex04_Load_function_in_builder();
        //await Ex05_basic_planner();
        //await Ex06_Use_math();
        //await Ex07_Use_python();
        await Ex08_Use_dotnet();
        Console.ReadLine();
    }

    public static async Task Ex02_InvokeLLMDirectly()
    {
        var builder = CreateBasicKernelBuilder();
        var kernel = builder.Build();
        FunctionResult result = await kernel.InvokePromptAsync("How are you today");
        if (result.Metadata.TryGetValue("Usage", out var usage))
        {
            if (usage is CompletionsUsage cu)
            {
                Console.WriteLine("Usage total token {0}, completion tokens {1} prompt tokens {2}", cu.TotalTokens, cu.CompletionTokens, cu.PromptTokens);
            }
        }

        var value = result.GetValue<OpenAIChatMessageContent>();
        Console.WriteLine("Model used: {0}", value.ModelId);
        Console.WriteLine(result);
    }

    public static async Task Ex02a_InvokeOpenaiClient()
    {
        var builder = CreateBasicKernelBuilder();
        var kernel = builder.Build();
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        ChatHistory chatMessages = new();
        chatMessages.AddUserMessage("Hi what is your name?");
        chatMessages.AddAssistantMessage("I am an Assistant ai but you can call me Jarvis");
        chatMessages.AddUserMessage("My name is Gian Maria");
        chatMessages.AddAssistantMessage("Hi Gian Maria how can I help you?");
        chatMessages.AddUserMessage("Tell my name and repeat how can I call you!");

        var result = await chatCompletionService.GetChatMessageContentAsync(chatMessages, new OpenAIPromptExecutionSettings()
        {
            MaxTokens = 122,
            Temperature = 0,
        });
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
        KernelArguments arguments = new KernelArguments()
        {
            ["messages"] = chatMessages,
            ExecutionSettings = new Dictionary<string, PromptExecutionSettings>()
            {
                ["default"] = new OpenAIPromptExecutionSettings()
                {
                    MaxTokens = 200,
                    Temperature = 0,
                    ModelId = "gpt35",
                }
            }
        };

        var result = await kernel.InvokeAsync<string>(prompt, arguments);
        Console.WriteLine("Result: {0}", result);

        var calls = _loggingProvider.GetLLMCalls();
    }

    public static async Task Ex02_c_InvokeLLMDirectly()
    {
        var builder = CreateBasicKernelBuilder();
        var kernel = builder.Build();

        // Create a template for chat with settings
        var chat = kernel.CreateFunctionFromPrompt(new PromptTemplateConfig()
        {
            Name = "Chat",
            Description = "Chat with the assistant.",
            Template = "{{$history}} User: {{$request}} Assistant: ",
            TemplateFormat = "semantic-kernel",
            InputVariables =
            [
                new() { Name = "history", Description = "The history of the conversation.", IsRequired = false, Default = "" },
                new() { Name = "request", Description = "The user's request.", IsRequired = true }
            ],
            ExecutionSettings =
            {
                { "default", new OpenAIPromptExecutionSettings()
                    {
                        MaxTokens = 1000,
                        Temperature = 0,
                        ModelId = "gpt4o",
                    }
                },
            }
        });

        StringBuilder history = new();
        history.AppendLine("You are an assistant that help user to find answer and your name is Jarvis");
        KernelArguments ka = new();
        ka["history"] = history.ToString();
        ka["request"] = "My name is Gian Maria, what is your name and purpose?";

        var result = await kernel.InvokeAsync(chat, ka);

        var calls = _loggingProvider.GetLLMCalls();

        AppendResult(history, result);

        ka = new();
        ka["history"] = history.ToString();
        ka["request"] = "I'd like to know which is the nearest star from our solar system?";
        result = await kernel.InvokeAsync(chat, ka);
        AppendResult(history, result);
        calls = _loggingProvider.GetLLMCalls();

        ka = new();
        ka["history"] = history.ToString();
        ka["request"] = "Do you know which is the position on hersprung russel diagram?";
        ka.ExecutionSettings = new Dictionary<string, PromptExecutionSettings>()
        {
            ["default"] = new OpenAIPromptExecutionSettings()
            {
                MaxTokens = 1000,
                Temperature = 0,
                ModelId = "gpt35",
            }
        };
        result = await kernel.InvokeAsync(chat, ka);
        calls = _loggingProvider.GetLLMCalls();

        foreach (LLMCall call in calls)
        {
            Console.WriteLine("Url: " + call.Url);
            Console.WriteLine("FullPrompt:\n" + call.FullRequest + "\n\n");
            Console.WriteLine("ResponseFunctionCall: " + call.ResponseFunctionCall);
            Console.WriteLine("Response: " + call.Response);
        }

        Console.WriteLine("Result: {0}", result);
    }

    public static async Task Ex02_c_InvokeLLWithTools()
    {
        var builder = CreateBasicKernelBuilder();
        var kernel = builder.Build();

        var function = KernelFunctionFactory.CreateFromMethod(
            [Description("Calculate a formula that contains standard operators")] (
                [Description("The formula, something like 4 / (5 ^ 2)")] string formula
            ) =>
        {
            return $"Called with formula {formula}";
        }, "math_formula");
        var plugin = KernelPluginFactory.CreateFromFunctions("MyPlugin", [function]);
        var openAIFunction = plugin.GetFunctionsMetadata().First().ToOpenAIFunction();

        // Create a template for chat with settings
        var chat = kernel.CreateFunctionFromPrompt(new PromptTemplateConfig()
        {
            Name = "Chat",
            Description = "Chat with the assistant.",
            Template = "{{$history}} User: {{$request}} Assistant: ",
            TemplateFormat = "semantic-kernel",
            InputVariables =
            [
                new() { Name = "history", Description = "The history of the conversation.", IsRequired = false, Default = "" },
                new() { Name = "request", Description = "The user's request.", IsRequired = true }
            ],
            ExecutionSettings =
            {
                { "default", new OpenAIPromptExecutionSettings()
                    {
                        MaxTokens = 1000,
                        Temperature = 0,
                        ModelId = "gpt4o",
                        ToolCallBehavior = ToolCallBehavior.RequireFunction(openAIFunction, false),
                    }
                },
            }
        });

        StringBuilder history = new();
        history.AppendLine("You are an assistant that help user to find answer and your name is Jarvis");
        KernelArguments ka = new();
        ka["history"] = history.ToString();
        ka["request"] = "Do you know the solution of (4 + 6) / 5?";
        var result = await kernel.InvokeAsync(chat, ka);

        Console.WriteLine("Result: {0}", result.ValueType);
        var openaiMessageContent = result.GetValue<OpenAIChatMessageContent>();

        if (result is FunctionResult fre)
        {
            var toolCall = openaiMessageContent.GetOpenAIFunctionToolCalls().Single();
            Console.WriteLine(
                "Function call: {0}({1})",
                toolCall.FunctionName,
                string.Join(',', toolCall.Arguments.Select(a => $"{a.Key}:{a.Value}")));
        }

        Console.WriteLine("Result: {0}", result);
    }

    public static async Task Ex02_d_InvokeLLMDirectly_handlebar()
    {
        var builder = CreateBasicKernelBuilder();
        var kernel = builder.Build();

        // Create a template for chat with settings
        var chat = kernel.CreateFunctionFromPrompt(new PromptTemplateConfig()
        {
            Name = "TestRewrite",
            Description = "Chat with the assistant.",
            Template = @"system: 
* Given the following conversation history and the users next question,rephrase the question to be a stand alone question.
If the conversation is irrelevant or empty, just restate the original question.
Do not add more details than necessary to the question.

chat history: 
{{#each history}}
question: 
{{question}}
answer: 
{{answer}}
{{/each}}

Follow up Input: {{ chat_input }} 
Standalone Question:",
            TemplateFormat = "handlebars",
            InputVariables =
            [
                new() { Name = "chat_input", Description = "New question of the user", IsRequired = false, Default = "" },
                new() { Name = "history", Description = "The history of the RAG CHAT.", IsRequired = true }
            ],
            ExecutionSettings =
            {
                { "default", new OpenAIPromptExecutionSettings()
                    {
                        MaxTokens = 1000,
                        Temperature = 0,
                        ModelId = "gpt35",
                    }
                },
            }
        },
        promptTemplateFactory: new HandlebarsPromptTemplateFactory());

        KernelArguments ka = new();
        ka["chat_input"] = "Do you know a similar technique?";

        ka["history"] = new RagChatElement[]
        {
            new ("Can you please explain complete mediation?", "Certainly! In the context of mediation, complete mediation refers to a situation where a mediator variable fully explains the relationship between an independent variable and a dependent variable. Without this mediator variable, no direct relationship is observed between the independent and dependent variables")
        };

        var result = await kernel.InvokeAsync(chat, ka);
        Console.WriteLine("result: {0}", result.ToString());

        var llmCalls = _loggingProvider.GetLLMCalls();
        foreach (var llmCall in llmCalls)
        {
            Console.WriteLine($"Function {llmCall.ResponseFunctionCall} with arguments {llmCall.ResponseFunctionCallParameters}");
        }
    }

    private class RagChatElement
    {
        public RagChatElement(string question, string answer)
        {
            Question = question;
            Answer = answer;
        }

        public string Question { get; set; }
        public string Answer { get; set; }
    }

    private static void AppendResult(StringBuilder history, FunctionResult result)
    {
        history.Append("Assistant: ");
        history.AppendLine(result.GetValue<string>());
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
        KernelPlugin audioVideoPlugin = kernel.ImportPluginFromObject(concreteAvPlugin, "AudioVideoPlugin");
        Console.WriteLine("Imported {0} functions from {1}", audioVideoPlugin.Count(), audioVideoPlugin.Name);

        audioVideoPlugin.TryGetFunction("ExtractAudio", out var extractAudio);
        KernelArguments args = new KernelArguments();
        args["videofile"] = @"C:\temp\sk\ssh.mp4";
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
            Temperature = 0,
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

    private static async Task Ex06_Use_math()
    {
        var kernelBuilder = CreateBasicKernelBuilder();
        kernelBuilder.Services.AddHttpClient();
        kernelBuilder
            .Plugins
                .AddFromType<ExpressionPlugin>("ExpressionPlugin")
                .AddFromType<Navigator>("NavigatorPlugin")
                .AddFromType<DuckDuckGo>("SearchPlugin");
        var kernel = kernelBuilder.Build();

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0,
        };

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var result = await chatCompletionService.GetChatMessageContentsAsync(
              "please tells me the result of 3 * (1 + 5 * 5)",
              executionSettings: openAIPromptExecutionSettings,
              kernel: kernel);
        Console.WriteLine("Result: {0}", result[result.Count - 1].Content);

        //var result = await chatCompletionService.GetChatMessageContentsAsync(
        //      "How can I configure Semantic Kernel in an Asp.net application?",
        //      executionSettings: openAIPromptExecutionSettings,
        //      kernel: kernel);
        //Console.WriteLine("Result: {0}", result[result.Count - 1].Content);

        //Console.WriteLine(result.ToString());
    }

    private static async Task Ex07_Use_python()
    {
        var kernelBuilder = CreateBasicKernelBuilder();
        kernelBuilder.Services.AddHttpClient();
        PythonExecutorConfiguration pythonExecutorConfiguration = new()
        {
            PythonLocation = @"A:\Develop\github\ai-notebooks\python\pywrapper\Scripts\python.exe"
        };
        kernelBuilder.Services.AddSingleton(pythonExecutorConfiguration);
        kernelBuilder
            .Plugins
                .AddFromType<PythonExecutor>("PythonExecutor");
        var kernel = kernelBuilder.Build();

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0,
            MaxTokens = 4096
        };

        KernelArguments arguments = new KernelArguments()
        {
            ExecutionSettings = new Dictionary<string, PromptExecutionSettings>()
            {
                ["default"] = openAIPromptExecutionSettings
            }
        };

        //ChatHistory history = new();
        //history.AddSystemMessage("You will answer question of the user, if you need to use a python script remember that the script will return output from print statement");

        //var result = await kernel.InvokePromptAsync<string>("I need to know the first 20 prime numbers", arguments);
        var result = await kernel.InvokePromptAsync<string>("I need to know what is contained in file S:\\Downloads\\Project-Management-Sample-Data.xlsx", arguments);
        Console.WriteLine(result.ToString());

        var llmCalls = _loggingProvider.GetLLMCalls();
        foreach (var llmCall in llmCalls)
        {
            Console.WriteLine(llmCall.Dump());
        }
        //var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        //var result = await chatCompletionService.GetChatMessageContentsAsync(
        //      "I need to know the first 20 prime numbers, use a python program to calculate them",
        //      executionSettings: openAIPromptExecutionSettings,
        //      kernel: kernel);
        //Console.WriteLine("Result: {0}", result[result.Count - 1].Content);

        //var result = await chatCompletionService.GetChatMessageContentsAsync(
        //      "How can I configure Semantic Kernel in an Asp.net application?",
        //      executionSettings: openAIPromptExecutionSettings,
        //      kernel: kernel);
        //Console.WriteLine("Result: {0}", result[result.Count - 1].Content);

        //Console.WriteLine(result.ToString());
    }

    private static async Task Ex08_Use_dotnet()
    {
        var kernelBuilder = CreateBasicKernelBuilder();
        kernelBuilder.Services.AddHttpClient();
        DotnetCommandExecutorConfig dotnetCommandExecutorConfig = new()
        {
            WorkingDirectory = @"c:\temp\skgenerated"
        };

        FileSystemPluginConfig fileSystemPluginConfig = new()
        {
            BaseDirectory = @"c:\temp\skgenerated"
        };
        kernelBuilder.Services.AddSingleton(dotnetCommandExecutorConfig);
        kernelBuilder.Services.AddSingleton(fileSystemPluginConfig);
        kernelBuilder
            .Plugins
                .AddFromType<DotnetCommandExecutor>("DotnetCommandExecutor")
                .AddFromType<FileSystemPlugin>("FileSystemPlugin");

        var kernel = kernelBuilder.Build();

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0,
            MaxTokens = 4096,
            ModelId = "gpt4omini"
        };

        KernelArguments arguments = new KernelArguments()
        {
            ExecutionSettings = new Dictionary<string, PromptExecutionSettings>()
            {
                ["default"] = openAIPromptExecutionSettings
            }
        };

        //var result = await kernel.InvokePromptAsync<string>("I need to know the first 20 prime numbers", arguments);
        //        var result = await kernel.InvokePromptAsync<string>(@"Generate a solution in C# that contains a project called EncryptorHelper with the following classes
        //AesKey: a simple wrapper around an AES key. This class should be able to be serialized to and from a file.
        //FileEncryptor: a class that allows encrypting a file given an AES key and a file, it will generate another file with the .encrypted extension

        //Then generate a test project with test classes written in xunit for the above two class.

        //Proceed in step, first of all generate a detailed plan for everything you need to do, then use the tool to actually generate the code.
        //Generates solution, then projects, then add project to the solution then make all test projects reference tested project, finally add the actual code. 
        //Remember to list all the nuget packages to use and add to the corresponding project.
        //Do not generate a console app.", arguments);

        var result = await kernel.InvokePromptAsync<string>(@"Generate a solution in C# that contains a project called EncryptorHelper with the following classes
AesKey: a simple wrapper around an AES key. This class should be able to be serialized to and from a file.
FileEncryptor: a class that allows encrypting a file given an AES key and a file, it will generate another file with the .encrypted extension

Then generate a test project with test classes written in xunit for the above two class.

Proceed in step, first of all generate a detailed plan for everything you need to do, then use the tool to actually generate the code.
Create solution project and nuget references for each project in a single call to create_solution_structure plugin.
Finally add the actual class code. 
Do not generate a console app. When you encouter the first error you will stop telling the user the error.", arguments);

        Console.WriteLine(result.ToString());

        var llmCalls = _loggingProvider.GetLLMCalls();
        foreach (var llmCall in llmCalls)
        {
            Console.WriteLine(llmCall.Dump());
        }
        //var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        //var result = await chatCompletionService.GetChatMessageContentsAsync(
        //      "I need to know the first 20 prime numbers, use a python program to calculate them",
        //      executionSettings: openAIPromptExecutionSettings,
        //      kernel: kernel);
        //Console.WriteLine("Result: {0}", result[result.Count - 1].Content);

        //var result = await chatCompletionService.GetChatMessageContentsAsync(
        //      "How can I configure Semantic Kernel in an Asp.net application?",
        //      executionSettings: openAIPromptExecutionSettings,
        //      kernel: kernel);
        //Console.WriteLine("Result: {0}", result[result.Count - 1].Content);

        //Console.WriteLine(result.ToString());
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

        kernelBuilder.Services.ConfigureHttpClientDefaults(c =>
        {
            c.AddLogger(s => _loggingProvider.CreateHttpRequestBodyLogger(s.GetRequiredService<ILogger<DumpLoggingProvider>>()));
            c.AddStandardResilienceHandler(options =>
            {
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(45);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
                // Configure standard resilience options here
            });
        });

        kernelBuilder.Services.AddAzureOpenAIChatCompletion(
            "GPT35_2",
            Dotenv.Get("OPENAI_API_BASE"),
            Dotenv.Get("OPENAI_API_KEY"),
            serviceId: "gpt35",
            modelId: "gpt35");

        kernelBuilder.Services.AddAzureOpenAIChatCompletion(
            "GPT4o", //"GPT35_2",//"GPT42",
            Dotenv.Get("OPENAI_API_BASE"),
            Dotenv.Get("OPENAI_API_KEY"),
            serviceId: "default",
            modelId: "gpt4o");

        kernelBuilder.Services.AddAzureOpenAIChatCompletion(
            "gpt-4o-mini", //"GPT35_2",//"GPT42",
            Dotenv.Get("OPENAI_SWEDEN_API_BASE"),
            Dotenv.Get("OPENAI_SWEDEN_API_KEY"),
            serviceId: "gpt4omini",
            modelId: "gpt4omini");

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

        var python = new PythonWrapper(@"A:\Develop\github\ai-notebooks\python\pywrapper\Scripts\python.exe");
        var script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python", "transcript_timeline.py");
        var result = python.Execute(script, @"C:\temp\ssh.wav");
        Console.WriteLine(result);
        return result;
    }
}