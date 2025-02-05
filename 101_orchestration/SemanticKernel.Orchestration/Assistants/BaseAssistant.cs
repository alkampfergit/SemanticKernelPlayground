using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SemanticKernel.Orchestration.Assistants;

/// <summary>
/// An assistant is capable of interacting with the kernel 
/// and orchestrating stuff.
/// </summary>
public abstract class BaseAssistant
{
    private readonly string _name;
    private readonly Dictionary<string, FunctionInfo> _functions = new(StringComparer.OrdinalIgnoreCase);

    private List<State> _state = new ();

    public record FunctionInfo(string Name, KernelFunction KernelFunction, Func<IDictionary<string, object>, Task<string>> Function);

    public BaseAssistant(string name)
    {
        _name = name;
    }

    protected void RegisterFunctionDelegate(
        string functionName,
        KernelFunction kernelFunction,
        Func<IDictionary<string, object>, Task<string>> function)
    {
        _functions[functionName] = new FunctionInfo(functionName, kernelFunction, function);
    }

    public virtual void AddStateToPrompt(ChatHistory chatHistory)
    {
        foreach (var state in _state)
        {
            chatHistory.AddMessage(
                AuthorRole.Assistant, 
                $"Function called: {state.functionName} with result: {state.result}");
        }
    }

    public IReadOnlyCollection<FunctionInfo> GetFunctions()
    {
        return _functions.Values;
    }

    public async Task ExecuteFunctionAsync(string function, IDictionary<string, object> arguments)
    {
        if (!_functions.ContainsKey(function))
        {
            throw new ArgumentException($"Function {function} not found");
        }

        var functionInfo = _functions[function];
        var result = await functionInfo.Function(arguments);
        _state.Add(new State(function, result));
    }

    private record State(string functionName, string result);
}
