using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Assistants;

/// <summary>
/// An assistant is capable of interacting with the kernel 
/// and orchestrating stuff.
/// </summary>
public abstract class BaseAssistant : IConversationOrchestrator
{
    private readonly string _name;
    private readonly Dictionary<string, string> _properties = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, FunctionInfo> _functions = new(StringComparer.OrdinalIgnoreCase);

    protected IConversationOrchestrator _orchestrator;

    public record FunctionInfo(string Name, KernelFunction KernelFunction, Func<IDictionary<string, object>, Task<AssistantResponse>> Function, bool IsFinal);

    public BaseAssistant(string name)
    {
        _name = name;
    }

    public string Name => _name;

    public virtual string InjectedPrompt => string.Empty;

    internal virtual void SetOrchestrator(IConversationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    protected void RegisterFunctionDelegate(
        string functionName,
        KernelFunction kernelFunction,
        Func<IDictionary<string, object>, Task<AssistantResponse>> function,
        bool isFinal = false)
    {
        _functions[functionName] = new FunctionInfo(functionName, kernelFunction, function, isFinal);
    }

    public virtual void AddResultToPrompt(ChatHistory chatHistory, AssistantResponse agentOperationResult)
    {
        chatHistory.AddAssistantMessage(agentOperationResult.Result);
    }

    public virtual string GetFact(AssistantResponse agentOperationResult)
    {
        return agentOperationResult.Result;
    }

    public IReadOnlyCollection<FunctionInfo> GetFunctions()
    {
        return _functions.Values;
    }

    public async Task<AssistantResponse> ExecuteFunctionAsync(string function, IDictionary<string, object> arguments)
    {
        if (!_functions.ContainsKey(function))
        {
            throw new ArgumentException($"Function {function} not found");
        }

        var functionInfo = _functions[function];
        return await functionInfo.Function(arguments);
    }

    public virtual string GetAssistantProperty(string propertyName)
    {
        if (_properties.TryGetValue(propertyName, out var value))
        {
            return value;
        }

        throw new ArgumentException($"Property {propertyName} not found");
    }

    protected void SetLocalProperty(string propertyName, string value)
    {
        _properties[propertyName] = value;
    }

    protected void SetGlobalProperty(string propertyName, string value)
    {
        _orchestrator.AddProperty(propertyName, value);
    }

    public void AddProperty(string propertyName, string value)
    {
        SetLocalProperty(propertyName, value);
    }

    public string? GetProperty([Description("Property name")] string propertyName)
    {
        return _properties.TryGetValue(propertyName, out var value) ? value : null;
    }
}
