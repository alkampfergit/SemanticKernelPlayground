using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SemanticKernel.Orchestration.Orchestrators;

public interface IConversation
{
    /// <summary>
    /// A conversation basic capability is creating a <see cref="ChatHistory"/> 
    /// object that represents the conversation history and can be passed
    /// to the basic Kernel object to interact with a LLM
    /// </summary>
    Task<ChatHistory> GetChatHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a user message to the conversation
    /// </summary>
    Task AddUserMessageAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an assistant message to the conversation
    /// </summary>
    Task AddAssistantMessageAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an assistant message to the conversation as an object
    /// </summary>
    Task AddAssistantMessageAsync(FunctionResult functionResult, CancellationToken cancellationToken = default);

    /// <summary>
    /// When you use the Chat interface of Semantic Kernel
    /// you will receive simple object results
    /// </summary>
    Task AddAssistantMessageAsync(object result, CancellationToken cancellationToken = default);
}

public abstract class BaseConversation : IConversation
{
    public async Task AddAssistantMessageAsync(FunctionResult functionResult, CancellationToken cancellationToken = default)
    {
        var openaiResponse = functionResult.GetValue<OpenAIChatMessageContent>();
        await OnAddOpenaiResponseAsync(openaiResponse, cancellationToken);
    }

    public async Task AddAssistantMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        await OnAddAssistantMessageAsync(message, cancellationToken);
    }

    public async Task AddUserMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        await OnAddUserMessageAsync(message, cancellationToken);
    }

    public async Task<ChatHistory> GetChatHistoryAsync(CancellationToken cancellationToken = default)
    {
        return await OnGetChatHistoryAsync(cancellationToken);
    }

    public async Task AddAssistantMessageAsync(object result, CancellationToken cancellationToken = default)
    {
        if (result is FunctionResult functionResult)
        {
            await AddAssistantMessageAsync(functionResult, cancellationToken);
        }
        else if (result is OpenAIChatMessageContent message)
        {
            await OnAddOpenaiResponseAsync(message, cancellationToken);
        }
        else if (result is string stringResult)
        {
            await OnAddAssistantMessageAsync(stringResult, cancellationToken);
        }
        else if (result is ChatMessageContent chatMessageContent)
        {
            await OnAddAssistantMessageAsync(chatMessageContent.Content!, cancellationToken);
        }
        else
        {
            throw new ArgumentException("Invalid object type for assistant message");
        }
    }

    protected abstract Task OnAddOpenaiResponseAsync(OpenAIChatMessageContent openaiResponse, CancellationToken cancellationToken);
    protected abstract Task OnAddAssistantMessageAsync(string message, CancellationToken cancellationToken);
    protected abstract Task OnAddUserMessageAsync(string message, CancellationToken cancellationToken);
    protected abstract Task<ChatHistory> OnGetChatHistoryAsync(CancellationToken cancellationToken);
}
