using SemanticMemory.Helper;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace AzureAiLibrary.Helpers.LogHelpers;

public class OpenAICallParser
{
    public LlmRequest ParseRequest(string fullContent)
    {
        //deserialize request
        var request = JsonSerializer.Deserialize<ApiPayload>(fullContent)!;
        StringBuilder prompt = new StringBuilder();
        foreach (var message in request.Messages)
        {
            if (message.Role == "user")
            {
                prompt.Append($"User: {message.Content}");
            }
            else if (message.Role == "assistant")
            {
                if (message.ToolCalls?.Any() == true)
                {
                    //TODO: is it possible to have multiple tool calls?
                    foreach (var toolCall in message.ToolCalls)
                    {
                        prompt.Append($"Tool call: {toolCall.Function.Name} with parameter: {toolCall.Function.Arguments}");
                    }
                }
                else
                {
                    prompt.Append($"Assistant: {message.Content}");
                }
            }
            else if (message.Role == "tool")
            {
                var relatedCall = request.Messages
                    .Where(m => m.ToolCalls?.Any() == true)
                    .Select(m => m.ToolCalls.FirstOrDefault(tc => tc.Id == message.ToolCallId))
                    .FirstOrDefault(c => c != null);
                prompt.Append($"Tool answer: {relatedCall?.Function?.Name} -> {message.Content}");
            }
            else if (message.Role == "system")
            {
                prompt.Append($"SYSTEM MESSAGE: {message.Content}");
            }
            prompt.Append("\n\n");
        }
        return new LlmRequest(fullContent, prompt.ToString());
    }

    public LlmResponse ParseResponse(string fullContent)
    {
        var response = JsonSerializer.Deserialize<ApiResponse>(fullContent)!;

        var choice = response.Choices.First();
        var message = choice.Message;

        List<ResponseFunctionCall> functionCalls = new List<ResponseFunctionCall>();
        if (message.ToolCalls?.Any() == true)
        {
            foreach (var toolCall in message.ToolCalls)
            {
                var functionCall = toolCall.Function.Name;
                var callArgument = toolCall.Function.Arguments;
                functionCalls.Add(new ResponseFunctionCall(functionCall, callArgument));
            }
        }

        var finishReason = choice.FinishReason;
        var answer = choice.Message?.Content ?? "";

        return new LlmResponse(fullContent, finishReason, functionCalls.ToArray(), response.Model, answer, response.Usage.PromptTokens, response.Usage.CompletionTokens, response.Usage.TotalTokens);
    }
}

public record LlmRequest(string FullContent, string PromptSequence);

public record LlmResponse(
    string FullContent,
    string FinishReason,
    ResponseFunctionCall[] FunctionCalls,
    string Model,
    string? Answer,
    int PromptTokens,
    int AnswerTokens,
    int TotalTokens);
