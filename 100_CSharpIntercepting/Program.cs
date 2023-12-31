using LogIntercepting.Helper;
using LogIntercepting.Helper.LogHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using SemanticKernelExperiments.plugins.SimpleMathPlugin;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LogIntercepting;

public static class Program
{
    private static DumpLoggingProvider _loggingProvider = new DumpLoggingProvider();

    static async Task Main(string[] args)
    {
        var kernelBuilder = CreateBasicKernelBuilder();
        kernelBuilder
            .Plugins
                .AddFromType<SimpleMathPlugin>("SimpleMathPlugin");
        var kernel = kernelBuilder.Build();

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        };

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        var correlationKey = Guid.NewGuid().ToString();
        _loggingProvider.StartCorrelation(correlationKey);
        ChatHistory chatMessages = new();
        chatMessages.AddUserMessage("I need you to calculate 2+4+8");
        var result = await chatCompletionService.GetChatMessageContentsAsync(
              chatMessages,
              executionSettings: openAIPromptExecutionSettings,
              kernel: kernel);

        Console.WriteLine("Result: {0}", result[result.Count - 1].Content);

        Console.WriteLine("Dump sequence of LLM calls");
        var dh = new DiagnoseHelper(_loggingProvider);
        var diagnoseResult = dh.Diagnose(correlationKey);

        for (int i = 0; i < diagnoseResult.Steps.Count; i++)
        {
            DiagnoseResult.Step step = diagnoseResult.Steps[i];
            Console.WriteLine($"Step: {i} - Stop reason: {step.AnswerType}");
            foreach (var functionCall in step.FunctionCalls)
            {
                Console.WriteLine($"\tFunction call: {functionCall.Function} arguments: {functionCall.Arguments}");
            }
        }

        Console.ReadLine();
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
            "GPT4t",
            Dotenv.Get("OPENAI_API_BASE"),
            Dotenv.Get("OPENAI_API_KEY"));

        return kernelBuilder;
    }
}