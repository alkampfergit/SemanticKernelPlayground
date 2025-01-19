using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Orchestrators;

public class SimpleConversation : BaseConversation
{
    private readonly ChatHistory _chatHistory;

    public SimpleConversation()
    {
        _chatHistory = new ChatHistory();
    }

    protected override Task OnAddOpenaiResponseAsync(OpenAIChatMessageContent openaiResponse, CancellationToken cancellationToken)
    {
        return OnAddAssistantMessageAsync(openaiResponse.Content!, cancellationToken);
    }

    protected override Task OnAddAssistantMessageAsync(string message, CancellationToken cancellationToken)
    {
        _chatHistory.AddAssistantMessage(message);
        return Task.CompletedTask;
    }

    protected override Task OnAddUserMessageAsync(string message, CancellationToken cancellationToken)
    {
        _chatHistory.AddUserMessage(message);
        return Task.CompletedTask;
    }

    protected override Task<ChatHistory> OnGetChatHistoryAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_chatHistory);
    }
}
