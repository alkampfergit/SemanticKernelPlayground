using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SemanticKernel.Orchestration.Helpers;

public class IChatCompletionServiceInterceptor : IChatCompletionService
{
    private readonly IChatCompletionService _inner;

    public IChatCompletionServiceInterceptor(
        IChatCompletionService inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IReadOnlyDictionary<string, object?> Attributes => _inner.Attributes;

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var container = InterceptorManager.GetActiveContainer();
        if (container != null)
        {
            // Check all wrappers first
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
        
        if (container != null)
        {
            // Call all interceptors after getting the result
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
