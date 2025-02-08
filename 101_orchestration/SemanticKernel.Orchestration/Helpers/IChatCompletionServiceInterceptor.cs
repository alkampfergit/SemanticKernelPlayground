using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticKernel.Orchestration.Orchestrators;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Helpers;

public class IChatCompletionServiceInterceptor : IChatCompletionService
{
    private readonly IChatCompletionService _inner;
    private readonly IEnumerable<IChatInterceptorTool> _interceptors;
    private readonly IEnumerable<IChatWrappingTool> _wrappers;

    public IChatCompletionServiceInterceptor(
        IChatCompletionService inner,
        IEnumerable<IChatInterceptorTool> interceptors,
        IEnumerable<IChatWrappingTool> wrappers)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _interceptors = interceptors ?? throw new ArgumentNullException(nameof(interceptors));
        _wrappers = wrappers ?? throw new ArgumentNullException(nameof(wrappers));
    }

    public IReadOnlyDictionary<string, object?> Attributes => _inner.Attributes;

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var container = KernelStore.GetActiveContainer();

        // Check all constructor-injected wrappers first
        foreach (var wrapper in _wrappers)
        {
            var wrappedResult = await wrapper.OnChatWrappingAsync(
                chatHistory,
                executionSettings,
                kernel,
                cancellationToken);

            if (wrappedResult != null)
            {
                return wrappedResult;
            }
        }

        // Then check container wrappers if available
        if (container != null)
        {
            foreach (var wrapper in container.Wrappers)
            {
                var wrappedResult = await wrapper.OnChatWrappingAsync(
                    chatHistory,
                    executionSettings,
                    kernel,
                    cancellationToken);

                if (wrappedResult != null)
                {
                    return wrappedResult;
                }
            }
        }

        var result = await _inner.GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);

        // Call all constructor-injected interceptors
        foreach (var interceptor in _interceptors)
        {
            await interceptor.OnChatCompletionAsync(
                result,
                chatHistory,
                executionSettings,
                kernel,
                cancellationToken);
        }

        // Then call container interceptors if available
        if (container != null)
        {
            foreach (var interceptor in container.Interceptors)
            {
                await interceptor.OnChatCompletionAsync(
                    result,
                    chatHistory,
                    executionSettings,
                    kernel,
                    cancellationToken);
            }
        }

        return result;
    }

    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        return _inner.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
    }
}
