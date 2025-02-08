using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Assistants.BaseAssistants;

internal class AnswerAssistant : BaseAssistant
{
    private readonly IReadOnlyCollection<BaseAssistant> _assistants;

    public AnswerAssistant(IReadOnlyCollection<BaseAssistant> assistants) : base("AnswerAssistant")
    {
        _assistants = assistants;

        RegisterFunctionDelegate(
            "GetAssistantProperty",
            KernelFunctionFactory.CreateFromMethod(GetAssistantProperty),
            async (args) => await GetAssistantProperty(
                args["assistantName"].ToString()!,
                args["propertyName"].ToString()!),
            isFinal: true);
    }

    [Description("Get a value from an assistant property to return to the user to answer the question and finish!")]
    private Task<string> GetAssistantProperty(
    [Description("The name of the assistant to get the property from")]
        string assistantName,
    [Description("The name of the property to get")]
        string propertyName)
    {
        var assistant = _assistants.FirstOrDefault(a => assistantName.Equals(a.Name, System.StringComparison.OrdinalIgnoreCase));
        if (assistant == null)
        {
            throw new ArgumentException($"Assistant {assistantName} not found");
        }

        return Task.FromResult(assistant.GetProperty(propertyName));
    }
}
