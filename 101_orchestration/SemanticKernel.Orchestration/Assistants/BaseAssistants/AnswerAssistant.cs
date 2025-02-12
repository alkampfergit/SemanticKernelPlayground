using Microsoft.SemanticKernel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Assistants.BaseAssistants;

internal class AnswerAssistant : BaseAssistant
{
    private readonly IReadOnlyCollection<BaseAssistant> _assistants;

    public AnswerAssistant(IReadOnlyCollection<BaseAssistant> assistants) : base("AnswerAssistant")
    {
        _assistants = assistants;

        RegisterFunctionDelegate(
            "GetProperty",
            KernelFunctionFactory.CreateFromMethod(GetProperty),
            async (args) => await GetProperty(args["propertyName"].ToString()!),
            isFinal: true);
    }

    [Description("Get a property value to return to the user to answer the question and finish!")]
    private Task<string?> GetProperty(
        [Description("The name of the property to get")]
        string propertyName)
    {
        return Task.FromResult(_orchestrator.GetProperty(propertyName));
    }
}
