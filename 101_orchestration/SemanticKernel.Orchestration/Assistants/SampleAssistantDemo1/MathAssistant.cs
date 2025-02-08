using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NCalc;

namespace SemanticKernel.Orchestration.Assistants.SampleAssistantDemo1;

public class MathAssistant : BaseAssistant
{
    public MathAssistant() : base("MathAssistant")
    {
        RegisterFunctionDelegate(
            "EvaluateExpression",
            KernelFunctionFactory.CreateFromMethod(EvaluateExpression),
            async (args) => await EvaluateExpression(args["expression"].ToString()!));
    }

    [Description("Evaluates a mathematical expression")]
    public async Task<string> EvaluateExpression(
        [Description("the expression to be evaluated")] string expression)
    {
        try
        {
            var expr = new NCalc.AsyncExpression(expression);
            var result = await expr.EvaluateAsync();
            return Convert.ToDouble(result).ToString();
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to evaluate expression: {expression}", ex);
        }
    }

    public override void AddStateToPrompt(ChatHistory chatHistory)
    {
        foreach (var state in _state)
        {
            chatHistory.AddAssistantMessage(state.Arguments["expression"].ToString() + " = " + state.Result);
        }
    }
}
