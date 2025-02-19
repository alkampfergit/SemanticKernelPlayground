using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Helpers;

public class CallLimiterTool : IChatWrappingTool
{
    private readonly int _maxCalls;

    private int _actualCalls;

    public CallLimiterTool(int maxCalls)
    {
        _maxCalls = maxCalls;
    }

    public Task<IReadOnlyList<ChatMessageContent>?> OnChatWrappingAsync(
        ChatHistory chatHistory, 
        PromptExecutionSettings? executionSettings, 
        Kernel? kernel,
        CancellationToken cancellationToken)
    {
        _actualCalls++;
        if (_actualCalls > _maxCalls)
        {
            var mockedResponse = new List<ChatMessageContent>
            {
                new ChatMessageContent(AuthorRole.Assistant, $"Max number of call reached: {_maxCalls}")
            };

            return Task.FromResult<IReadOnlyList<ChatMessageContent>?>(mockedResponse);
        }

        return Task.FromResult<IReadOnlyList<ChatMessageContent>?>(null);
    }
}
