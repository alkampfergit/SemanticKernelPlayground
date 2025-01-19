using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticKernel.Orchestration.Orchestrators;

namespace SemanticKernel.Orchestration.Assistants;

public class SimpleChatAssistant
{
    private readonly IConversation _conversation;
    private readonly string _kernelName;
    private readonly KernelStore _kernelStore;

    public SimpleChatAssistant(
        string kernelName,
        KernelStore kernelStore)
    {
        _conversation = new SimpleConversation();
        _kernelName = kernelName;
        _kernelStore = kernelStore;
    }

    public async Task<string> SendMessageAsync(string message)
    {
        _conversation.AddUserMessage(message);
        
        var kernel = _kernelStore.GetKernel(_kernelName);
        
        var chatHistory = _conversation.GetChatHistory();
        var ccs = kernel.GetRequiredService<IChatCompletionService>();
        var results = await ccs.GetChatMessageContentsAsync(chatHistory);
        
        var result = results.Single();
        _conversation.AddAssistantMessage(result);
        
        return result.ToString()!;
    }
}
