using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Helpers;

public class SingleModelTokenUsage 
{
    public int TotalTokens { get; private set; }
    public int PromptTokens { get; private set; }
    public int CompletionTokens { get; private set; }

    internal void AddUsage(OpenAI.Chat.ChatTokenUsage usage)
    {
        TotalTokens += usage.TotalTokenCount;
        PromptTokens += usage.InputTokenCount;
        CompletionTokens += usage.OutputTokenCount;
    }
}

public class ModelTokenUsage
{
    private Dictionary<string, SingleModelTokenUsage> _modelUsage = new();

    public IReadOnlyDictionary<string, SingleModelTokenUsage> ModelUsageList => _modelUsage;

    public string LastCallModel { get; set; }

    public int LastCallTotalTokens { get; set; }
    public int LastCallPromptTokens { get; set; }
    public int LastCallCompletionTokens { get; set; }

    internal void AddUsage(string modelName, OpenAI.Chat.ChatTokenUsage usage)
    {
        if (!_modelUsage.TryGetValue(modelName, out var singleModelUsage))
        {
            _modelUsage[modelName] = singleModelUsage = new SingleModelTokenUsage();
        }

        singleModelUsage.AddUsage(usage);
        LastCallModel = modelName;
        LastCallTotalTokens = usage.TotalTokenCount;
        LastCallPromptTokens = usage.InputTokenCount;
        LastCallCompletionTokens = usage.OutputTokenCount;
    }
}

public class TokenUsageCounter : IChatInterceptorTool
{
    public int CallCount { get; private set; }

    public ModelTokenUsage ModelTokenUsage { get; private set; } = new();

    private TokenUsagePrinter _usagePrinter;

    public void SetUsagePrinter(TokenUsagePrinter usagePrinter)
    {
        _usagePrinter = usagePrinter;
    }

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
        CallCount++;

        foreach (var item in returnValue)
        {
            if (item is OpenAIChatMessageContent ocmc)
            {
                if (ocmc.Metadata?.TryGetValue("Usage", out var completionUsage) == true
                    && completionUsage is OpenAI.Chat.ChatTokenUsage usage)
                {
                    string modelName = GetModelName(ocmc);
                    ModelTokenUsage.AddUsage(modelName, usage);
                }
            }
        }

        _usagePrinter?.Print(this);
        return Task.CompletedTask;
    }
}