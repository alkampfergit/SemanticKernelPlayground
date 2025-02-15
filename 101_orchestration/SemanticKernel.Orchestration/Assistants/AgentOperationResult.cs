using System;
using System.Collections.Generic;

namespace SemanticKernel.Orchestration.Assistants;

public record AgentOperationResult(
    string FunctionName,
    IDictionary<string, object> Arguments,
    string Result,
    object? State = null)
{
    public string ToPromptString() => $"Tool called: {FunctionName} with parameters {String.Join(",", Arguments)} returned: {Result}";
}

public record AssistantResponse(string Result, object? State = null)
{
    public static implicit operator AssistantResponse(string result) => new AssistantResponse(result);
}
