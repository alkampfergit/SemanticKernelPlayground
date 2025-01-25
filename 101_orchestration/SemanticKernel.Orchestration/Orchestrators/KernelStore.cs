using System;
using System.Collections.Generic;
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel;

namespace SemanticKernel.Orchestration.Orchestrators;

public class KernelInfo
{
    private Kernel _kernel;

    public KernelInfo(IKernelBuilder builder, ModelInformation modelName)
    {
        Builder = builder;
        ModelInformation = modelName;
    }

    public IKernelBuilder Builder { get; }
    public ModelInformation ModelInformation { get; }

    public Kernel Kernel => _kernel ??= Builder.Build();
}

public class KernelStore
{
    private readonly Dictionary<string, KernelInfo> _kernels = new();

    public void AddKernel(
        string name,
        IKernelBuilder kernelBuilder,
        ModelInformation modelName)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        _kernels[name] = new KernelInfo(kernelBuilder, modelName);
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

    private KernelInfo GetKernelInfo(string name)
    {
        if (!_kernels.TryGetValue(name, out var info))
        {
            throw new KeyNotFoundException($"Kernel '{name}' not found");
        }
        return info;
    }

    public Kernel GetKernel(string name)
    {
        return GetKernelInfo(name).Kernel;
    }

    public TiktokenTokenizer GetKernelTokenizer(string name)
    {
        return GetKernelInfo(name).ModelInformation.Tokenizer;
    }

    public void AddPlugin(string kernelName, object plugin)
    {
        if (!_kernels.TryGetValue(kernelName, out var kernel))
            throw new KeyNotFoundException($"Kernel '{kernelName}' not found");

        kernel.Builder.Plugins.AddFromObject(plugin);
    }
}
