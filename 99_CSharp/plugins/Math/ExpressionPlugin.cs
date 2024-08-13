using Microsoft.SemanticKernel;
using NCalc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelExperiments.plugins.Math;

[Description("Simple plugin to evaluate mathematical expressions")]
internal class ExpressionPlugin
{
    [KernelFunction("evaluate")]
    [Description("Evaluates a mathematical expression")]
    public double Evaluate([Description("The expression to evaluate, like 2 + 4 ^ 5")] string expression)
    {
        Console.WriteLine("We are executing Evaluate with expression: {0}", expression);
        var expr = new Expression(expression);
        Func<double> f = expr.ToLambda<double>();
        return f();
    }
}
