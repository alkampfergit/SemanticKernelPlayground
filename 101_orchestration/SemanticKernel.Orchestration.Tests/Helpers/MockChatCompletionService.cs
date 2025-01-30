using Azure.AI.OpenAI;
using Fasterflect;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Tests.Helpers;

public class MockChatCompletionService : IChatCompletionService
{
    private Func<ChatHistory, Task<IReadOnlyList<ChatMessageContent>>>? _chatResponseGenerator;

    private Func<ChatHistory, IAsyncEnumerable<StreamingChatMessageContent>>? _streamingResponseGenerator;

    private IReadOnlyDictionary<string, object?> _attributes = new Dictionary<string, object?>();

    public IReadOnlyDictionary<string, object?> Attributes => _attributes;

    public MockChatCompletionService()
    {
        SetChatMockedResponse("Dummy response");
    }

    public void SetResponseGenerator(Func<ChatHistory, Task<IReadOnlyList<ChatMessageContent>>> generator)
    {
        _chatResponseGenerator = generator;
    }

    /// <summary>
    /// Comèletely override the response, model will return this message regardless of the 
    /// ChatMessage input
    /// </summary>
    /// <param name="responses"></param>
    public void SetMockResponse(params string[] responses)
    {
        int i = 0;
        _chatResponseGenerator =
        (history) => Task.FromResult<IReadOnlyList<ChatMessageContent>>([new ChatMessageContent(
            AuthorRole.Assistant,
            content: responses[(int) Math.Min(i++ , responses.Length - 1)]
        )]);
    }

    /// <summary>
    /// Will use the default responder that will dump chat message and append this message
    /// </summary>
    /// <param name="mockedResponse"></param>
    internal void SetChatMockedResponse(string mockedResponse)
    {
        _chatResponseGenerator =
        (history) => Task.FromResult<IReadOnlyList<ChatMessageContent>>([new ChatMessageContent(
            AuthorRole.Assistant,
            content: $"Chat history:\n{FormatChatHistory(history)}\n{mockedResponse}"
        )]);
    }

    internal void SetMockResponseTool(
        string pluginName,
        string methodName,
        IDictionary<string, object?> arguments)
    {
        FunctionCallContent callContent = new FunctionCallContent(
            functionName: methodName,
            pluginName: pluginName,
            arguments: new KernelArguments(arguments),
            id: Guid.NewGuid().ToString()
        );
        _chatResponseGenerator =
            (history) => Task.FromResult<IReadOnlyList<ChatMessageContent>>([
                new ChatMessageContent()
                {
                    Items =  [callContent],
                    Role = AuthorRole.Assistant,
                    Metadata = new Dictionary<string, object?>()
                    {
                        { "FunctionCall", callContent }
                    },
                    InnerContent = callContent,     
                }
            ]);
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

    private static string FormatChatHistory(ChatHistory history)
    {
        return string.Join("\n", history.Select(m => $"{m.Role}: {m.Content}"));
    }
}
