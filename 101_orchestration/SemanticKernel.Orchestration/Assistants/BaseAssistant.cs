using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Assistants;

/// <summary>
/// An assistant is capable of interacting with the kernel 
/// and orchestrating stuff.
/// </summary>
public abstract class BaseAssistant
{
    private readonly string _name;
    private readonly Dictionary<string, string> _properties = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, FunctionInfo> _functions = new(StringComparer.OrdinalIgnoreCase);

    protected AssistantBasedOrchestrator _orchestrator;

    protected List<State> _stateList = new();

    public record FunctionInfo(string Name, KernelFunction KernelFunction, Func<IDictionary<string, object>, Task<string>> Function, bool IsFinal);

    public BaseAssistant(string name)
    {
        _name = name;
    }

    public string Name => _name;

    internal void SetOrchestrator(AssistantBasedOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    protected void RegisterFunctionDelegate(
        string functionName,
        KernelFunction kernelFunction,
        Func<IDictionary<string, object>, Task<string>> function,
        bool isFinal = false)
    {
        _functions[functionName] = new FunctionInfo(functionName, kernelFunction, function, isFinal);
    }

    public virtual void AddStateToPrompt(ChatHistory chatHistory)
    {
        foreach (var state in _stateList)
        {
            chatHistory.AddAssistantMessage(state.ToPromptString());
        }
    }

    public virtual List<string> GetFacts()
    {
        var facts = new List<string>();
        foreach (var state in _stateList)
        {
            facts.Add(state.ToPromptString());
        }
        return facts;
    }

    public IReadOnlyCollection<FunctionInfo> GetFunctions()
    {
        return _functions.Values;
    }

    public async Task<string> ExecuteFunctionAsync(string function, IDictionary<string, object> arguments)
    {
        if (!_functions.ContainsKey(function))
        {
            throw new ArgumentException($"Function {function} not found");
        }

        var functionInfo = _functions[function];
        var result = await functionInfo.Function(arguments);
        _stateList.Add(new State(function, arguments, result));
        return result;
    }

    public virtual string GetProperty(string propertyName)
    {
        if (_properties.TryGetValue(propertyName, out var value))
        {
            return value;
        }

        throw new ArgumentException($"Property {propertyName} not found");
    }

    protected void SetProperty(string propertyName, string value)
    {
        _properties[propertyName] = value;
    }

    protected record State(string FunctionName, IDictionary<string, object> Arguments, string Result)
    {
        public string ToPromptString() => $"Tool called: {FunctionName} with parameters {String.Join(",", Arguments)} returned: {Result}";
    }
}
