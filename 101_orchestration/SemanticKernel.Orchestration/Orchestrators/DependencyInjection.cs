using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SemanticKernel.Orchestration.Assistants;

namespace SemanticKernel.Orchestration.Orchestrators;

public record KernelDefinition(string Name, IKernelBuilder Builder, string Description);

public static class DependencyInjection
{
    /// <summary>
    /// Add a kernel store configured with a list of 
    /// kernel definitions
    /// </summary>
    /// <param name="services"></param>
    /// <param name="kernelDefinitions"></param>
    /// <returns></returns>
    public static IServiceCollection AddKernelStore(
        this IServiceCollection services,
        IEnumerable<KernelDefinition> kernelDefinitions)
    {
        services.AddSingleton(sp =>
        {
            var kernelStore = new KernelStore(sp);

            foreach (var definition in kernelDefinitions)
            {
                kernelStore.AddKernel(
                    definition.Name, 
                    definition.Builder, 
                    ModelInformation.GPT4O, 
                    definition.Description);
            }
            
            kernelStore.EnableInterception();
            return kernelStore;
        });
        return services;
    }
}
