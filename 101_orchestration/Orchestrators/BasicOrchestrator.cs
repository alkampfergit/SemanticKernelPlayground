using System;
using System.Collections.Generic;
using Microsoft.SemanticKernel;

namespace SemanticKernel.Orchestration.Orchestrators;

public class BasicOrchestrator
{
    private readonly Dictionary<string, IKernelBuilder> _kernelBuilders = new();

    public BasicOrchestrator()
    {
    }

    public void AddKernel(
        string name, 
        IKernelBuilder kernelBuilder)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
            
        _kernelBuilders[name] = kernelBuilder;
    }

    public void AddPlugin<T>(string kernelName, object plugin) 
    {
        if (!_kernelBuilders.TryGetValue(kernelName, out var builder))
            throw new KeyNotFoundException($"Kernel '{kernelName}' not found");

        builder.Plugins.AddFromObject(plugin);
    }

    
}
