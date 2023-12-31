using Microsoft.SemanticKernel;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace SemanticKernelExperiments.plugins.SimpleMathPlugin;

public class SimpleMathPlugin
{
    [
        KernelFunction, 
        Description("Add two numbers and return the result"),
    ]
    public  double Add(
        [Description("First number")] double first,
        [Description("Second number")] double second)
    {
        Console.WriteLine($"Calling Add {first} + {second}");    
        return first + second;
    }
}
