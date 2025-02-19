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
    private readonly TokenUsageCounter _counter;
    private readonly Dictionary<string, (decimal InputCost, decimal OutputCost)> _modelCosts;

    public TokenUsagePrinter(
        TokenUsageCounter counter,
        Dictionary<string, (decimal InputCost, decimal OutputCost)>? modelCosts = null)
    {
        _counter = counter;
        _modelCosts = modelCosts ?? new Dictionary<string, (decimal, decimal)>();
    }

    public TokenUsageReport GetUsageReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("\nToken usage per model:");

        decimal totalCost = 0m;
        decimal lastCallCost = 0m;

        foreach (var modelUsage in _counter.ModelTokenUsage.ModelUsageList)
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
        if (_counter.ModelTokenUsage.LastCallModel != null)
        {
            var lastCallModel = _counter.ModelTokenUsage.LastCallModel;
            var lastCallUsage = _counter.ModelTokenUsage;
            if (_modelCosts.TryGetValue(lastCallModel, out var costs))
            {
                lastCallCost = (lastCallUsage.LastCallPromptTokens * costs.InputCost) +
                                (lastCallUsage.LastCallCompletionTokens * costs.OutputCost);
            }
            sb.Append($"\nLast call model: {lastCallModel}");
            sb.Append($" Total: {lastCallUsage.LastCallTotalTokens}, " +
                          $" Prompt: {lastCallUsage.LastCallPromptTokens}, " +
                          $" Completion: {lastCallUsage.LastCallCompletionTokens}");
            if (lastCallCost > 0)
            {
                sb.Append($" Cost: ${lastCallCost:F8}");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"\nTotal calls: {_counter.CallCount}");
        sb.AppendLine($"Total cost across all models: ${totalCost:F8}");

        return new TokenUsageReport(sb.ToString(), totalCost, lastCallCost);
    }
}
