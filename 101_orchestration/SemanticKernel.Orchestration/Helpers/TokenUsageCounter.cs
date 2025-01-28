using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SemanticKernel.Orchestration.Helpers;

public class TokenUsageCounter : IChatInterceptorTool
{
    private int _totalTokens = 0;
    private int _promptTokens = 0;
    private int _completionTokens = 0;
    private int _callCount = 0;
    private int _lastTotalTokens = 0;
    private int _lastPromptTokens = 0;
    private int _lastCompletionTokens = 0;
    
    public int TotalTokens => _totalTokens;
    public int PromptTokens => _promptTokens;
    public int CompletionTokens => _completionTokens;
    public int CallCount => _callCount;
    public int LastTotalTokens => _lastTotalTokens;
    public int LastPromptTokens => _lastPromptTokens;
    public int LastCompletionTokens => _lastCompletionTokens;

    public Task OnChatCompletionAsync(
        IReadOnlyList<ChatMessageContent> returnValue,
        ChatHistory chatHistory,
        PromptExecutionSettings executionSettings,
        Kernel kernel,
        CancellationToken cancellationToken)
    {
        _callCount++;
        _lastTotalTokens = 0;
        _lastPromptTokens = 0;
        _lastCompletionTokens = 0;

        foreach (var item in returnValue)
        {
            if (item is OpenAIChatMessageContent ocmc)
            {
                if (ocmc.Metadata.TryGetValue("Usage", out var completionUsage)
                    && completionUsage is CompletionsUsage usage)
                {
                    _lastTotalTokens += usage.TotalTokens;
                    _lastPromptTokens += usage.PromptTokens;
                    _lastCompletionTokens += usage.CompletionTokens;
                    
                    _totalTokens += usage.TotalTokens;
                    _promptTokens += usage.PromptTokens;
                    _completionTokens += usage.CompletionTokens;
                }
            }
        }
        
        return Task.CompletedTask;
    }
}

