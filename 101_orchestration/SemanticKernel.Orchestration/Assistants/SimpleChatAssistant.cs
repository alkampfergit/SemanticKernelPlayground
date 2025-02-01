using Microsoft.SemanticKernel.ChatCompletion;
using SemanticKernel.Orchestration.Orchestrators;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Assistants;

/// <summary>
/// This is an assistant that implements a simple chat
/// based conversation with a user.
/// </summary>
public class SimpleChatAssistant
{
    private readonly IConversation _conversation;
    private readonly string _kernelName;
    private readonly KernelStore _kernelStore;

    public SimpleChatAssistant(
        string kernelName,
        KernelStore kernelStore,
        IConversation? conversation = null)
    {
        _conversation = conversation ?? new SimpleConversation();
        _kernelName = kernelName;
        _kernelStore = kernelStore;
    }

    public async Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        await _conversation.AddUserMessageAsync(message, cancellationToken);

        var kernel = _kernelStore.GetKernel(_kernelName);

        var chatHistory = await _conversation.GetChatHistoryAsync(cancellationToken);
        var ccs = kernel.GetRequiredService<IChatCompletionService>();
        var results = await ccs.GetChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken);

        var result = results.Single();
        await _conversation.AddAssistantMessageAsync(result, cancellationToken);

        return result.ToString()!;
    }
}
