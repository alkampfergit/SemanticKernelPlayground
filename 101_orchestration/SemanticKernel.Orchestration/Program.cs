using Microsoft.Extensions.DependencyInjection;
using SemanticKernel.Orchestration.Assistants;
using SemanticKernel.Orchestration.Assistants.BaseAssistants;
using SemanticKernel.Orchestration.Assistants.SampleAssistantDemo1;
using SemanticKernel.Orchestration.Helpers;
using SemanticKernel.Orchestration.Orchestrators;
using SemanticKernelExperiments.AudioVideoPlugin;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration;

public static class Program
{
    static async Task Main(string[] args)
    {
        IServiceCollection serviceCollection = new ServiceCollection();

        // Test with GPT4o
        var gpt4oBuilder = Configuration.SemanticKernelConfigurator
            .CreateBasicKernelBuilderGpt4o();

        // Test with GPT4 Mini
        var gpt4MiniBuilder = Configuration.SemanticKernelConfigurator
            .CreateBasicKernelBuilderGpt4Mini();

        // register the interceptors you want to use, register the
        // kernel store in the global service collection
        serviceCollection.AddTransient<IChatInterceptorTool, TokenUsageCounter>();
        serviceCollection.AddKernelStore(new[]
        {
            new KernelDefinition(
                "gpt4o",
                gpt4oBuilder,
                "GPT-4 Optimized Kernel for enhanced performance"),
            new KernelDefinition(
                "gpt4omini",
                gpt4MiniBuilder,
                "GPT-4 Mini Kernel for lighter workloads")
        });

        serviceCollection.AddTransient(sp =>
        {
            var abo = new AssistantBasedOrchestrator(sp.GetRequiredService<KernelStore>());
            abo.AddAssistant(new MathAssistant());
            return abo;
        });

        serviceCollection.AddKeyedTransient<SummaryAssistant>("audiovideo");

        serviceCollection.AddKeyedTransient("audiovideo", (sp, key) =>
        {
            var abo = new AssistantBasedOrchestrator(sp.GetRequiredService<KernelStore>());
            var summaryAssystant = sp.GetRequiredKeyedService<SummaryAssistant>("audiovideo");
            abo.AddAssistant(summaryAssystant);
            abo.AddAssistant(new AudioVideoAssistant());
            return abo;
        });

        serviceCollection.AddSingleton<IChatInterceptorTool, TokenUsageCounter>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        //var kernelStore = serviceProvider.GetRequiredService<KernelStore>();
        ////await SimpleChatExampleAsync(kernelStore);
        //bool shouldExit;
        //do
        //{
        //    using var scope = kernelStore.StartContainerScope();
        //    var compressedConversation = new TokenLimitedConversation(kernelStore, "gpt4omini", 2000);
        //    shouldExit = await SimpleChatExampleAsync(kernelStore, compressedConversation);
        //} while (!shouldExit);

        //orchestrator example
        //await OrchestratorSimpleMathExampleAsync(serviceProvider);
        await OrchestratorVideoExampleAsync(serviceProvider);
    }

    /// <summary>
    ///
    /// I want to extract audio from video "C:\temp\rdp.mp4"
    /// 
    /// I need transcription of "C:\temp\rdp.mp4"
    ///
    /// I need a summary of the audio of "C:\temp\rdp.mp4"
    ///
    /// whisper C:\temp\rdp.wav --task transcribe --output_format txt --output_dir c:\temp\out --model tiny
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    private static async Task OrchestratorVideoExampleAsync(ServiceProvider serviceProvider)
    {
        var orchestrator = serviceProvider.GetRequiredKeyedService<AssistantBasedOrchestrator>("audiovideo");
        var kernelStore = serviceProvider.GetRequiredService<KernelStore>();
        while (true)
        {
            using var scope = kernelStore.StartContainerScope();
            Console.Write("\nAsk a question (press Enter or type 'exit' to quit): ");
            var question = Console.ReadLine();

            if (string.IsNullOrEmpty(question) || question.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Goodbye!");
                break;
            }

            try
            {
                var answer = await orchestrator.AskAsync(question);
                Console.WriteLine("\nAnswer: " + answer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private static async Task OrchestratorSimpleMathExampleAsync(ServiceProvider serviceProvider)
    {
        var orchestrator = serviceProvider.GetRequiredService<AssistantBasedOrchestrator>();
        var kernelStore = serviceProvider.GetRequiredService<KernelStore>();
        while (true)
        {
            using var scope = kernelStore.StartContainerScope();
            Console.Write("\nAsk a question (press Enter or type 'exit' to quit): ");
            var question = Console.ReadLine();

            if (string.IsNullOrEmpty(question) || question.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Goodbye!");
                break;
            }

            try
            {
                var answer = await orchestrator.AskAsync(question);
                Console.WriteLine("\nAnswer: " + answer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private static async Task<bool> SimpleChatExampleAsync(
        KernelStore kernelStore,
        IConversation? conversation = null)
    {
        var tokenCounter = kernelStore.GetInterceptor<TokenUsageCounter>();
        var usagePrinter = new TokenUsagePrinter(tokenCounter, new Dictionary<string, (decimal, decimal)>
        {
            { "gpt-4", (0.06m/1000, 0.06m/1000) },           // GPT-4
            { "gpt-4-turbo", (0.04m/1000, 0.04m/1000) },     // GPT-4 Turbo
            { "gpt-4o", (0.08m/1000, 0.08m/1000) },           // GPT-4o
            { "gpt-4o-mini", (0.03m/1000, 0.03m/1000) }         // GPT-4o Mini
        });

        var assistant = new SimpleChatAssistant("gpt4omini", kernelStore, conversation);
        while (true)
        {
            Console.Write("\nYou: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrEmpty(userInput))
                continue;

            var loweredInput = userInput.ToLower();
            if (loweredInput == "exit")
            {
                Console.WriteLine("Chat ended. Goodbye!");
                return true;
            }
            else if (loweredInput == "clear")
            {
                Console.WriteLine("Chat history cleared. Starting new conversation...");
                return false;
            }

            var response = await assistant.SendMessageAsync(userInput);
            Console.WriteLine("\nAssistant: " + response);

            var usageReport = usagePrinter.GetUsageReport();
            Console.WriteLine(usageReport.FormattedReport);
        }
    }
}
