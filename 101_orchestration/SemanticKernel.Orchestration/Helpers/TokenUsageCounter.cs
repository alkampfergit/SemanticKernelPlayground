using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Helpers;

public class ModelTokenUsage
{
    public int TotalTokens { get; private set; }
    public int PromptTokens { get; private set; }
    public int CompletionTokens { get; private set; }
    public int LastTotalTokens { get; private set; }
    public int LastPromptTokens { get; private set; }
    public int LastCompletionTokens { get; private set; }

    internal void AddUsage(OpenAI.Chat.ChatTokenUsage usage)
    {
        LastTotalTokens = usage.TotalTokenCount;
        LastPromptTokens = usage.InputTokenCount;
        LastCompletionTokens = usage.OutputTokenCount;

        TotalTokens += usage.TotalTokenCount;
        PromptTokens += usage.InputTokenCount;
        CompletionTokens += usage.OutputTokenCount;
    }
}

public class TokenUsageCounter : IChatInterceptorTool
{
    public TokenUsageCounter()
    {
            
    }

    private readonly Dictionary<string, ModelTokenUsage> _modelUsage = new();
    private int _callCount = 0;

    public int CallCount => _callCount;
    public IReadOnlyDictionary<string, ModelTokenUsage> ModelUsage => _modelUsage;

    private string GetModelName(OpenAIChatMessageContent message)
    {
        if (message.InnerContent is OpenAI.Chat.ChatCompletion chatCompletion)
        {
            return CleanModelName(chatCompletion.Model);
        }
        return string.Empty;
    }

    private string CleanModelName(string modelName)
    {
        if (string.IsNullOrEmpty(modelName)) return string.Empty;

        if (modelName.Length >= 10)
        {
            string lastTenChars = modelName.Substring(modelName.Length - 10);
            // Check if the last 10 characters match YYYY-MM-DD pattern
            // using standard datetime try parse
            if (DateTime.TryParse(lastTenChars, out _))
            {
                return modelName.Substring(0, modelName.Length - 11);
            }
        }
        return modelName;
    }

    public Task OnChatCompletionAsync(
        IReadOnlyList<ChatMessageContent> returnValue,
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings,
        Kernel? kernel,
        CancellationToken cancellationToken)
    {
        _callCount++;

        foreach (var item in returnValue)
        {
            if (item is OpenAIChatMessageContent ocmc)
            {
                if (ocmc.Metadata?.TryGetValue("Usage", out var completionUsage) == true
                    && completionUsage is OpenAI.Chat.ChatTokenUsage usage)
                {
                    string modelName = GetModelName(ocmc);

                    if (!_modelUsage.ContainsKey(modelName))
                    {
                        _modelUsage[modelName] = new ModelTokenUsage();
                    }

                    _modelUsage[modelName].AddUsage(usage);
                }
            }
        }

        return Task.CompletedTask;
    }
}

