using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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

        //now sample with the simple kernel router
        serviceCollection.AddSingleton<KernelStore>();

        // register the interceptors you want to use, register the
        // kernel store in the global service collection
        serviceCollection.AddTransient<IChatInterceptorTool, TokenUsageCounter>();
        serviceCollection.AddSingleton(sp =>
        {
            var kernelStore = new KernelStore(sp);

            kernelStore.AddKernel("gpt4o", gpt4oBuilder, ModelInformation.GPT4O, "gpt4o based kernel");
            kernelStore.AddKernel("gpt4mini", gpt4MiniBuilder, ModelInformation.GPT4O, "gpt4mini based kernel");

            kernelStore.EnableInterception();
            return kernelStore;
        });

        // now build the provider so we can get the KernelStore and
        // configure with the handlers
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var kernelStore = serviceProvider.GetRequiredService<KernelStore>();

        //await SimpleChatExampleAsync(kernelStore);
        var compressedConversation = new TokenLimitedConversation(kernelStore, "gpt4mini", 2000);
        await SimpleChatExampleAsync(kernelStore, compressedConversation);
    }

    private static async Task SimpleChatExampleAsync(
        KernelStore kernelStore,
        IConversation? conversation = null)
    {
        // Start interactive chat loop
        using (var scope = kernelStore.StartContainerScope())
        {

            var tokenCounter = kernelStore.GetInterceptor<TokenUsageCounter>();
            var assistant = new SimpleChatAssistant("gpt4mini", kernelStore, conversation);
            while (true)
            {
                Console.Write("\nYou: ");
                var userInput = Console.ReadLine();

                if (string.IsNullOrEmpty(userInput) || userInput.ToLower() == "exit")
                {
                    Console.WriteLine("Chat ended. Goodbye!");
                    break;
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
}
