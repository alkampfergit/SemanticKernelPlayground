using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticKernel.Orchestration.Helpers;

public record TokenUsageReport(
    string FormattedReport,
    decimal TotalCost,
    decimal LastCallCost
);

public class TokenUsagePrinter
{
    private readonly Dictionary<string, (decimal InputCost, decimal OutputCost)> _modelCosts;

    public TokenUsagePrinter(
        Dictionary<string, (decimal InputCost, decimal OutputCost)>? modelCosts = null)
    {
        _modelCosts = modelCosts ?? new Dictionary<string, (decimal, decimal)>();
    }

    public void Print(TokenUsageCounter tokenUsageCounter)
    {
        var report = GetUsageReport(tokenUsageCounter);
        Console.WriteLine(report.FormattedReport);
    }

    public TokenUsageReport GetUsageReport(TokenUsageCounter tokenUsageCounter)
    {
        var sb = new StringBuilder();
        decimal totalCost = 0m;
        decimal lastCallCost = 0m;
        sb.AppendLine($"\nTotal calls: {tokenUsageCounter.CallCount}");
        foreach (var modelUsage in tokenUsageCounter.ModelTokenUsage.ModelUsageList)
        {
            var model = modelUsage.Key;
            var usage = modelUsage.Value;

            // Cumulative statistics
            decimal modelTotalCost = 0m;

            // Cost calculation if available for this model
            if (_modelCosts.TryGetValue(model, out var costs))
            {
                modelTotalCost = (usage.PromptTokens * costs.InputCost) +
                                   (usage.CompletionTokens * costs.OutputCost);

                totalCost += modelTotalCost;
            }

            sb.Append($"Model: {model} - Total: {usage.TotalTokens}, " +
                         $"Prompt: {usage.PromptTokens}, " +
                         $"Completion: {usage.CompletionTokens}");
            if (modelTotalCost > 0)
            {
                sb.Append($", Cost: ${modelTotalCost:F8}");
            }
            sb.AppendLine();
        }

        // Last call statistics
        if (tokenUsageCounter.ModelTokenUsage.LastCallModel != null)
        {
            var lastCallModel = tokenUsageCounter.ModelTokenUsage.LastCallModel;
            var lastCallUsage = tokenUsageCounter.ModelTokenUsage;
            if (_modelCosts.TryGetValue(lastCallModel, out var costs))
            {
                lastCallCost = (lastCallUsage.LastCallPromptTokens * costs.InputCost) +
                                (lastCallUsage.LastCallCompletionTokens * costs.OutputCost);
            }
            sb.Append($"Last call model: {lastCallModel}");
            sb.Append($"Total: {lastCallUsage.LastCallTotalTokens}, " +
                          $" Prompt: {lastCallUsage.LastCallPromptTokens}, " +
                          $" Completion: {lastCallUsage.LastCallCompletionTokens}");
            if (lastCallCost > 0)
            {
                sb.Append($" Cost: ${lastCallCost:F8}");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"Total cost across all models: ${totalCost:F8}");

        return new TokenUsageReport(sb.ToString(), totalCost, lastCallCost);
    }
}
