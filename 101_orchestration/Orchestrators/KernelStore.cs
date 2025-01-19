using System;
using System.Collections.Generic;
using Microsoft.SemanticKernel;

namespace SemanticKernel.Orchestration.Orchestrators;

public record KernelInfo(IKernelBuilder Builder, string Description);

public class KernelStore
{
    private readonly Dictionary<string, KernelInfo> _kernels = new();

    public void AddKernel(
        string name,
        IKernelBuilder kernelBuilder,
        string description)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        if (string.IsNullOrEmpty(description))
            throw new ArgumentNullException(nameof(description));

        _kernels[name] = new KernelInfo(kernelBuilder, description);
    }

    public bool TryGetKernel(string name, out KernelInfo kernel)
    {
        return _kernels.TryGetValue(name, out kernel);
    }

    public IReadOnlyDictionary<string, KernelInfo> GetAllKernels()
    {
        return _kernels;
    }

    public void AddPlugin<T>(string kernelName, object plugin)
    {
        if (!_kernels.TryGetValue(kernelName, out var kernel))
            throw new KeyNotFoundException($"Kernel '{kernelName}' not found");

        kernel.Builder.Plugins.AddFromObject(plugin);
    }
}
