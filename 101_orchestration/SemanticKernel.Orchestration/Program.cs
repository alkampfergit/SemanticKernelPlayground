using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SemanticKernel.Orchestration.Assistants;
using SemanticKernel.Orchestration.Helpers;
using SemanticKernel.Orchestration.Orchestrators;

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
        serviceCollection.AddKernelStore(
            new Dictionary<string, IKernelBuilder>
            {
                {"gpt4o", gpt4oBuilder},
                {"gpt4mini", gpt4MiniBuilder}
            }
        );

        // now build the provider so we can get the KernelStore and
        // configure with the handlers
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var kernelStore = serviceProvider.GetRequiredService<KernelStore>();

        //await SimpleChatExampleAsync(kernelStore);
        bool shouldExit;
        do
        {
            using var scope = kernelStore.StartContainerScope();
            var compressedConversation = new TokenLimitedConversation(kernelStore, "gpt4mini", 2000);
            shouldExit = await SimpleChatExampleAsync(kernelStore, compressedConversation);
        } while (!shouldExit);
    }

    private static async Task<bool> SimpleChatExampleAsync(
        KernelStore kernelStore,
        IConversation? conversation = null)
    {
        // Start interactive chat loop

        var tokenCounter = kernelStore.GetInterceptor<TokenUsageCounter>();
        var assistant = new SimpleChatAssistant("gpt4mini", kernelStore, conversation);
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
            Console.WriteLine($"\nLast call stats - Total: {tokenCounter.LastTotalTokens}, " +
                            $"Prompt: {tokenCounter.LastPromptTokens}, " +
                            $"Completion: {tokenCounter.LastCompletionTokens}");
            Console.WriteLine($"Cumulative stats after {tokenCounter.CallCount} calls - " +
                            $"Total: {tokenCounter.TotalTokens}, " +
                            $"Prompt: {tokenCounter.PromptTokens}, " +
                            $"Completion: {tokenCounter.CompletionTokens}");
    }
}
}
