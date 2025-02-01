using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SemanticKernel.Orchestration.Assistants;

namespace SemanticKernel.Orchestration.Orchestrators;

public static class DependencyInjection
{
    /// <summary>
    /// Add a kernel store configured with a list of 
    /// kernel builder
    /// </summary>
    /// <param name="services"></param>
    /// <param name="kernelBuilders"></param>
    /// <returns></returns>
    public static IServiceCollection AddKernelStore(
        this IServiceCollection services,
        Dictionary<string, IKernelBuilder> kernelBuilders)
    {
        services.AddSingleton(sp =>
        {
            var kernelStore = new KernelStore(sp);

            foreach (var (kernelName, kernelBuilder) in kernelBuilders)
            {
                kernelStore.AddKernel(kernelName, kernelBuilder, ModelInformation.GPT4O, $"{kernelName} based kernel");
            }
            
            kernelStore.EnableInterception();
            return kernelStore;
        });
        return services;
    }
}
