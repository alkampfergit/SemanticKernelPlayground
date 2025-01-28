using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SemanticKernel.Orchestration.Helpers;

public interface IChatInterceptorTool
{
    Task OnChatCompletionAsync(
        IReadOnlyList<ChatMessageContent> returnValue,
        ChatHistory chatHistory,
        PromptExecutionSettings executionSettings,
        Kernel kernel,
        CancellationToken cancellationToken);
}

public static class InterceptorManager
{
    private static AsyncLocal<TokenUsageCounter> _currentCounter = new();

    public static TokenUsageCounter CreateCounter()
    {
        var counter = new TokenUsageCounter();
        _currentCounter.Value = counter;
        return counter;
    }

    public static TokenUsageCounter? GetActiveCounter()
    {
        return _currentCounter.Value;
    }

    public static void ClearCounter()
    {
        _currentCounter.Value = null;
    }
}
