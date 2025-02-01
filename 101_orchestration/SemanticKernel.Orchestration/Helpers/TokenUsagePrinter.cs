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

        foreach (var modelUsage in _counter.ModelUsage)
        {
            var model = string.IsNullOrEmpty(modelUsage.Key) ? "unknown" : modelUsage.Key;
            var usage = modelUsage.Value;
            
            sb.AppendLine($"Model: {model}");
            
            // Last call statistics
            sb.AppendLine($"  Last call - Total: {usage.LastTotalTokens}, " +
                         $"Prompt: {usage.LastPromptTokens}, " +
                         $"Completion: {usage.LastCompletionTokens}");

            // Cumulative statistics
            sb.AppendLine($"  Cumulative - Total: {usage.TotalTokens}, " +
                         $"Prompt: {usage.PromptTokens}, " +
                         $"Completion: {usage.CompletionTokens}");

            // Cost calculation if available for this model
            if (_modelCosts.TryGetValue(model, out var costs))
            {
                var modelLastCallCost = (usage.LastPromptTokens * costs.InputCost) + 
                                      (usage.LastCompletionTokens * costs.OutputCost);
                var modelTotalCost = (usage.PromptTokens * costs.InputCost) + 
                                   (usage.CompletionTokens * costs.OutputCost);
                
                lastCallCost += modelLastCallCost;
                totalCost += modelTotalCost;
                
                sb.AppendLine($"  Costs - Last Call: ${modelLastCallCost:F8}, " +
                             $"Total: ${modelTotalCost:F8}");
            }
        }

        sb.AppendLine($"\nTotal calls: {_counter.CallCount}");
        sb.AppendLine($"Total cost across all models: ${totalCost:F8}");
        sb.AppendLine($"Last call cost across all models: ${lastCallCost:F8}");

        return new TokenUsageReport(sb.ToString(), totalCost, lastCallCost);
    }
}
