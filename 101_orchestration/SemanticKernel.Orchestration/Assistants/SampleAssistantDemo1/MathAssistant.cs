using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using NCalc;

namespace SemanticKernel.Orchestration.Assistants.SampleAssistantDemo1;

public class MathAssistant : BaseAssistant
{
    public MathAssistant() : base("MathAssistant")
    {
        RegisterFunctionDelegate(
            "EvaluateExpression",
            KernelFunctionFactory.CreateFromMethod(EvaluateExpression),
            async (args) =>
            {
            return await EvaluateExpression(args["expression"].ToString()!);
            });
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
}
