using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace SemanticKernel.Orchestration.Helpers;

/// <summary>
/// Interface for ac omponent that will be called after the chat completion
/// and could inspect the input parameters as well as the respondse content
/// of the LLM
/// </summary>
public interface IChatInterceptorTool
{
    Task OnChatCompletionAsync(
        IReadOnlyList<ChatMessageContent> returnValue,
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings,
        Kernel? kernel,
        CancellationToken cancellationToken);
}

/// <summary>
/// This interface will be called before calling the real LLM implementation
/// and if it returns a NON null response, that response will be returned to 
/// the caller, this will allow for testing and some advanced scenario
/// </summary>
public interface IChatWrappingTool 
{
    /// <summary>
    /// This method will be called before the real chat implementation and if
    /// it returns a NON null response, that response will be returned to the caller
    /// </summary>
    /// <param name="chatHistory"></param>
    /// <param name="executionSettings"></param>
    /// <param name="kernel"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<ChatMessageContent>?> OnChatWrappingAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings,
        Kernel? kernel,
        CancellationToken cancellationToken);
}

public class InterceptorContainer : IDisposable
{
    public IChatInterceptorTool[] Interceptors { get; }
    public IChatWrappingTool[] Wrappers { get; }

    public InterceptorContainer(
        IChatInterceptorTool[] interceptors,
        IChatWrappingTool[] wrappers)
    {
        Interceptors = interceptors;
        Wrappers = wrappers;
    }

    public void Dispose()
    {
        foreach (var interceptor in Interceptors)
        {
            if (interceptor is IDisposable disposableInterceptor)
            {
                disposableInterceptor.Dispose();
            }
        }

        foreach (var wrapper in Wrappers)
        {
            if (wrapper is IDisposable disposableWrapper)
            {
                disposableWrapper.Dispose();
            }
        }

        InterceptorManager.ClearContainer();
    }
}

public class InterceptorManager
{
    private readonly IServiceProvider _serviceProvider;
    private static AsyncLocal<InterceptorContainer> _currentContainer = new();

    public InterceptorManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public InterceptorContainer CreateContainer()
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
}
