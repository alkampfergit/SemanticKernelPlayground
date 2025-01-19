using System;
using System.Collections.Generic;
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
    /// <returns></returns>
    ChatHistory GetChatHistory();

    /// <summary>
    /// Adds a user message to the conversation
    /// </summary>
    /// <param name="message">The message to add</param>
    void AddUserMessage(string message);

    /// <summary>
    /// Adds an assistant message to the conversation
    /// </summary>
    /// <param name="message">The message to add</param>
    void AddAssistantMessage(string message);

    /// <summary>
    /// Adds an assistant message to the conversation as an object
    /// </summary>
    /// <param name="functionResult">The object to add as message content</param>
    void AddAssistantMessage(FunctionResult functionResult);

    /// <summary>
    /// When you use the Chat interface of Semantic Kernel
    /// you will recedive simple object results
    /// </summary>
    /// <param name="result"></param>
    void AddAssistantMessage(object result);
}

public abstract class BaseConversation : IConversation
{
    public void AddAssistantMessage(FunctionResult functionResult)
    {
        var openaiResponse = functionResult.GetValue<OpenAIChatMessageContent>();
        OnOpenaiResponse(openaiResponse);
    }

    public void AddAssistantMessage(string message)
    {
        OnAssistantMessage(message);
    }

    public void AddUserMessage(string message)
    {
        OnUserMessage(message);
    }

    public ChatHistory GetChatHistory()
    {
        return OnGetChatHistory();
    }

    public void AddAssistantMessage(object result)
    {
        if (result is FunctionResult functionResult)
        {
            // call the specific function for kernel response.
            AddAssistantMessage(functionResult);
        }
        else if (result is OpenAIChatMessageContent message)
        {
            OnOpenaiResponse(message);
        }
        else if (result is string stringResult)
        {
            OnAssistantMessage(stringResult);
        }
        else
        {
            throw new ArgumentException("Invalid object type for assistant message");
        }
    }

    protected abstract void OnOpenaiResponse(OpenAIChatMessageContent openaiResponse);
    protected abstract void OnAssistantMessage(string message);
    protected abstract void OnUserMessage(string message);
    protected abstract ChatHistory OnGetChatHistory();
}
