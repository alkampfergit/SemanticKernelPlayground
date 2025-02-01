using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel;
using SemanticKernel.Orchestration.Helpers;

namespace SemanticKernel.Orchestration.Orchestrators;

/// <summary>
/// Contains information about a Kernel object of Semantic Kernel
/// </summary>
public class KernelInfo
{
    private Kernel _kernel;

    public KernelInfo(IKernelBuilder builder, ModelInformation modelName, string description, string name)
    {
        Builder = builder;
        ModelInformation = modelName;
        Description = description;
        Name = name;
    }

    public IKernelBuilder Builder { get; }
    public ModelInformation ModelInformation { get; }
    public string Description { get; }
    public string Name { get; }

    public Kernel Kernel => _kernel ??= Builder.Build();
}

public class KernelStore
{
    private readonly Dictionary<string, KernelInfo> _kernels = new();
    private readonly IServiceProvider _serviceProvider;
    private static AsyncLocal<InterceptorContainer?> _currentContainer = new();

    public KernelStore(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void AddKernel(
        string name,
        IKernelBuilder kernelBuilder,
        ModelInformation modelName,
        string description)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        _kernels[name] = new KernelInfo(kernelBuilder, modelName, description, name);
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

    public void AddInterceptor<T>(string kernelName) where T : class, IChatInterceptorTool
    {
        if (!_kernels.TryGetValue(kernelName, out var kernelInfo))
            throw new KeyNotFoundException($"Kernel '{kernelName}' not found");

        kernelInfo.Builder.Services.WithInterceptorTransient<T>();
    }

    public void AddWrapper<T>(string kernelName) where T : class, IChatWrappingTool
    {
        if (!_kernels.TryGetValue(kernelName, out var kernelInfo))
            throw new KeyNotFoundException($"Kernel '{kernelName}' not found");

        kernelInfo.Builder.Services.WithWrapperTransient<T>();
    }

    public void AddGlobalInterceptor<T>() where T : class, IChatInterceptorTool
    {
        foreach (var kernelInfo in _kernels.Values)
        {
            kernelInfo.Builder.Services.WithInterceptorTransient<T>();
        }
    }

    public void AddGlobalWrapper<T>() where T : class, IChatWrappingTool
    {
        foreach (var kernelInfo in _kernels.Values)
        {
            kernelInfo.Builder.Services.WithWrapperTransient<T>();
        }
    }

    public IEnumerable<KernelInfo> GetAvailableKernels(string excludeKernel = null)
    {
        return _kernels
            .Where(k => k.Key != excludeKernel)
            .Select(k => k.Value);
    }

    public void EnableInterception()
    {
        foreach (var kernelInfo in _kernels.Values)
        {
            kernelInfo.Builder.EnableInterception();
        }
    }

    public InterceptorContainer StartContainerScope()
    {
        var interceptors = _serviceProvider.GetServices<IChatInterceptorTool>().ToArray();
        var wrappers = _serviceProvider.GetServices<IChatWrappingTool>().ToArray();

        var container = new InterceptorContainer(interceptors, wrappers);
        _currentContainer.Value = container;
        return container;
    }

    public static InterceptorContainer? GetActiveContainer()
    {
        return _currentContainer.Value;
    }

    internal static void ClearContainer()
    {
        _currentContainer.Value = null;
    }

    internal T? GetInterceptor<T>() where T : class
    {
        var container = _currentContainer.Value;
        if (container == null)
        {
            return null;
        }

        return container.Interceptors.OfType<T>().FirstOrDefault()
            ?? container.Wrappers.OfType<T>().FirstOrDefault();
    }
}
