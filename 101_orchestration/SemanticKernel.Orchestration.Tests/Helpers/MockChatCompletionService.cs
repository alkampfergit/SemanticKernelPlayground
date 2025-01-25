using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SemanticKernel.Orchestration.Tests.Helpers;

public class MockChatCompletionService : IChatCompletionService
{
    private Func<ChatHistory, Task<IReadOnlyList<ChatMessageContent>>>? _chatResponseGenerator =
        (_) => Task.FromResult<IReadOnlyList<ChatMessageContent>>([new ChatMessageContent(
            AuthorRole.Assistant,
            content: "Dummy response"
        )]);
    private Func<ChatHistory, IAsyncEnumerable<StreamingChatMessageContent>>? _streamingResponseGenerator;

    private IReadOnlyDictionary<string, object?> _attributes = new Dictionary<string, object?>();

    public IReadOnlyDictionary<string, object?> Attributes => _attributes;

    public void SetResponseGenerator(Func<ChatHistory, Task<IReadOnlyList<ChatMessageContent>>> generator)
    {
        _chatResponseGenerator = generator;
    }

    public void SetMockResponse(string response) {
        _chatResponseGenerator = (_) => Task.FromResult<IReadOnlyList<ChatMessageContent>>([new ChatMessageContent(
            AuthorRole.Assistant,
            content: response
        )]);
    }

    public void SetStreamingResponseGenerator(Func<ChatHistory, IAsyncEnumerable<StreamingChatMessageContent>> generator)
    {
        _streamingResponseGenerator = generator;
    }

    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        if (_chatResponseGenerator == null)
        {
            throw new InvalidOperationException("Response generator not set");
        }

        return _chatResponseGenerator(chatHistory);
    }

    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        if (_streamingResponseGenerator == null)
        {
            throw new InvalidOperationException("Streaming response generator not set");
        }

        return _streamingResponseGenerator(chatHistory);
    }
}
