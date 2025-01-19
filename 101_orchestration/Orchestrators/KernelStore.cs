using System;
using System.Collections.Generic;
using Microsoft.SemanticKernel;

namespace SemanticKernel.Orchestration.Orchestrators;

public class KernelInfo
{
    private Kernel _kernel;

    public KernelInfo(IKernelBuilder builder, string description)
    {
        Builder = builder;
        Description = description;
    }

    public IKernelBuilder Builder { get; }
    public string Description { get; }

    public Kernel Kernel => _kernel ??= Builder.Build();
}

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

    public bool TryGetKernel(string name, out Kernel? kernel)
    {
        if (_kernels.TryGetValue(name, out var info))
        {
            kernel = info.Kernel;
        }
        else
        {
            kernel = null;
        }
        return kernel != null;
    }

    public Kernel GetKernel(string name)
    {
        if (!_kernels.TryGetValue(name, out var info))
        {
            throw new KeyNotFoundException($"Kernel '{name}' not found");
        }
        return info.Kernel;
    }

    public void AddPlugin(string kernelName, object plugin)
    {
        if (!_kernels.TryGetValue(kernelName, out var kernel))
            throw new KeyNotFoundException($"Kernel '{kernelName}' not found");

        kernel.Builder.Plugins.AddFromObject(plugin);
    }
}
