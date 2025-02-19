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

/// <summary>
/// When TerminateCycle is true, the agent want to terminate the cycle, let the user prosecute
/// with questions, and finally we need to print the state to the user or if the state is null
/// print Result
/// </summary>
/// <param name="Result"></param>
/// <param name="State"></param>
/// <param name="TerminateCycle"></param>
public record AssistantResponse(string Result, object? State = null, bool TerminateCycle = false)
{
    public static implicit operator AssistantResponse(string result) => new AssistantResponse(result);
}
