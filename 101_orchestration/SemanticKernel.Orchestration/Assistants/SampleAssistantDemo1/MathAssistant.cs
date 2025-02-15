using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

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

    private int count = 1;

    [Description("Evaluates a mathematical expression")]
    public async Task<AssistantResponse> EvaluateExpression(
        [Description("the expression to be evaluated")] string expression)
    {
        try
        {
            var expr = new NCalc.AsyncExpression(expression);
            var result = await expr.EvaluateAsync();
            var resultDouble = Convert.ToDouble(result);

            var propertyName = $"expression{count++}";
            base._orchestrator.AddProperty(propertyName, resultDouble.ToString());
            var expressionResult = new ExpressionResult(propertyName, expression, resultDouble);
            return new AssistantResponse(
                $"Result of {expression} is in property {propertyName} and it is equal to {resultDouble}",
                expressionResult);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to evaluate expression: {expression}", ex);
        }
    }

    public override string GetFact(AssistantResponse agentOperationResult)
    {
        return ((ExpressionResult)agentOperationResult.State!).ToFact();
    }

    public override void AddResultToPrompt(ChatHistory chatHistory, AssistantResponse agentOperationResult)
    {
        chatHistory.AddAssistantMessage(((ExpressionResult)agentOperationResult.State!).ToAssistantMessage());
    }

    private record ExpressionResult(string PropertyName, string Expression, double Result)
    {
        public string ToFact() => $"Result of {Expression} is in property {PropertyName} and it is equal to {Result}";

        public string ToAssistantMessage() => $"Result of {Expression} is {Result}";
    }
}
