using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace SemanticKernel.Orchestration;

public static class Program
{
    static async Task Main(string[] args)
    {
        var prompt = "tell me an haiku about cat and akita inu";
        
        // Test with GPT4o
        var gpt4oBuilder = Configuration.SemanticKernelConfigurator.CreateBasicKernelBuilderGpt4o();
        var kernelGpt4o = gpt4oBuilder.Build();
        var answerGpt4o = await kernelGpt4o.InvokePromptAsync(prompt);
        Console.WriteLine("GPT-4 Response:");
        Console.WriteLine(answerGpt4o.ToString());
        
        // Test with GPT4 Mini
        var gpt4MiniBuilder = Configuration.SemanticKernelConfigurator.CreateBasicKernelBuilderGpt4Mini();
        var kernelGpt4Mini = gpt4MiniBuilder.Build();
        var answerGpt4Mini = await kernelGpt4Mini.InvokePromptAsync(prompt);
        Console.WriteLine("\nGPT-4 Mini Response:");
        Console.WriteLine(answerGpt4Mini.ToString());
    }
}
