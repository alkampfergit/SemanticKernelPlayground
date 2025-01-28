using System;
using System.Threading.Tasks;
using SemanticKernel.Orchestration.Assistants;
using SemanticKernel.Orchestration.Helpers;
using SemanticKernel.Orchestration.Orchestrators;

namespace SemanticKernel.Orchestration;

public static class Program
{
    static async Task Main(string[] args)
    {
        // Test with GPT4o
        var gpt4oBuilder = Configuration.SemanticKernelConfigurator
            .CreateBasicKernelBuilderGpt4o()
            .EnableInterception();
        // var kernelGpt4o = gpt4oBuilder.Build();
        // var answerGpt4o = await kernelGpt4o.InvokePromptAsync(prompt);
        // Console.WriteLine("GPT-4 Response:");
        // Console.WriteLine(answerGpt4o.ToString());

        // Test with GPT4 Mini
        var gpt4MiniBuilder = Configuration.SemanticKernelConfigurator
            .CreateBasicKernelBuilderGpt4Mini()
            .EnableInterception();
        // var kernelGpt4Mini = gpt4MiniBuilder.Build();
        // var answerGpt4Mini = await kernelGpt4Mini.InvokePromptAsync(prompt);
        // Console.WriteLine("\nGPT-4 Mini Response:");
        // Console.WriteLine(answerGpt4Mini.ToString());

        //now sample with the simple kernel router
        var kernelStore = new Orchestrators.KernelStore();
        kernelStore.AddKernel("gpt4o", gpt4oBuilder, ModelInformation.GPT4O, "gpt4o based kernel");
        kernelStore.AddKernel("gpt4mini", gpt4MiniBuilder, ModelInformation.GPT4O, "gpt4mini based kernel");

        //await SimpleChatExampleAsync(kernelStore);

        var compressedConversation = new TokenLimitedConversation(kernelStore, "gpt4mini", 2000);
        await SimpleChatExampleAsync(kernelStore, compressedConversation);
    }

    private static async Task SimpleChatExampleAsync(
        KernelStore kernelStore,
        IConversation conversation = null)
    {
        // Start interactive chat loop
        var tokenCounter = InterceptorManager.CreateCounter();
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
