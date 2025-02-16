using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace SemanticKernel.Orchestration.Configuration;

public class SemanticKernelConfigurator
{
    public static IKernelBuilder CreateBasicKernelBuilderGpt4o(bool withLogging = false)
    {
        return ConfigureBasicKernelBuilder("GPT4o", "gpt4o", "gpt4o", withLogging);
    }

    public static IKernelBuilder CreateBasicKernelBuilderGpt4Mini(bool withLogging = false)
    {
        return ConfigureBasicKernelBuilder("GPT4omini", "GPT4omini", "GPT4omini", withLogging);
    }

    private static IKernelBuilder ConfigureBasicKernelBuilder(string deploymentName, string serviceId, string modelId, bool withLogging)
    {
        var kernelBuilder = Kernel.CreateBuilder();
        
        if (withLogging)
        {
            kernelBuilder.Services.AddLogging(l => l
                .SetMinimumLevel(LogLevel.Trace)
                .AddConsole()
                .AddDebug()
            );
        }

        kernelBuilder.Services.AddAzureOpenAIChatCompletion(
            deploymentName,
            Dotenv.Get("OPENAI_API_BASE"),
            Dotenv.Get("OPENAI_API_KEY"),
            serviceId: serviceId,
            modelId: modelId);

        return kernelBuilder;
    }
}
