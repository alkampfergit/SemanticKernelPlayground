using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Orchestrators;

public class TokenLimitedConversation : SimpleConversation
{
    private readonly ChatHistory _chatHistory;
    private readonly Kernel _kernel;
    private readonly TiktokenTokenizer _tokenizer;
    private readonly int _maxTokens;

    public TokenLimitedConversation(
        KernelStore kernelStore,
        string kernelName,
        int maxTokens = 2000)
    {
        _kernel = kernelStore.GetKernel(kernelName);
        _tokenizer = kernelStore.GetKernelTokenizer(kernelName);
        _maxTokens = maxTokens;
        _chatHistory = new ChatHistory();
    }

    private async Task CompressHistoryIfNeededAsync(CancellationToken cancellationToken)
    {
        if (_chatHistory.Count == 0)
        {
            return;
        }

        var tokenCount = _chatHistory
            .Select(m => _tokenizer.CountTokens(m.Content))
            .Sum();

        if (tokenCount > _maxTokens)
        {
            var summarizer = _kernel.CreateFunctionFromPrompt(
                "Summarize this conversation between a user and an assistant while keeping the most important points: {{$input}}");

            var ka = new KernelArguments();
            string conversationText = string.Join("\n", _chatHistory.Select(m => $"{m.Role}: {m.Content}"));
            ka.Add("input", conversationText);
            var summary = await _kernel.InvokeAsync(summarizer, ka, cancellationToken);

            _chatHistory.Clear();
            _chatHistory.AddSystemMessage($"Conversation so far:\n{summary.ToString()}\n\n");
        }
    }

    protected override async Task OnAddOpenaiResponseAsync(OpenAIChatMessageContent openaiResponse, CancellationToken cancellationToken)
    {
        _chatHistory.AddAssistantMessage(openaiResponse.Content!);
        await CompressHistoryIfNeededAsync(cancellationToken);
    }

    protected override async Task OnAddAssistantMessageAsync(string message, CancellationToken cancellationToken)
    {
        _chatHistory.AddAssistantMessage(message);
        await CompressHistoryIfNeededAsync(cancellationToken);
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
