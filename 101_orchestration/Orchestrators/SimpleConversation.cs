using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SemanticKernel.Orchestration.Orchestrators;

public class SimpleConversation : BaseConversation
{
    private readonly ChatHistory _chatHistory;

    public SimpleConversation()
    {
        _chatHistory = new ChatHistory();
    }

    protected override void OnOpenaiResponse(OpenAIChatMessageContent openaiResponse)
    {
        _chatHistory.AddAssistantMessage(openaiResponse.Content!);
    }

    protected override void OnAssistantMessage(string message)
    {
        _chatHistory.AddAssistantMessage(message);
    }

    protected override void OnUserMessage(string message)
    {
        _chatHistory.AddUserMessage(message);
    }

    protected override ChatHistory OnGetChatHistory()
    {
        return _chatHistory;
    }
}
