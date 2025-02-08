using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticKernel.Orchestration.Assistants.BaseAssistants;
using SemanticKernel.Orchestration.Orchestrators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Assistants;

public class AssistantBasedOrchestrator
{
    private const string DefaultModelName = "gpt4omini";
    private readonly KernelStore _kernelStore;
    private readonly List<BaseAssistant> _assistants;

    public AssistantBasedOrchestrator(KernelStore kernelStore)
    {
        _kernelStore = kernelStore;
        _assistants = new List<BaseAssistant>();

        //Add some default assistants
        _assistants.Add(new AnswerAssistant(_assistants));
    }

    public AssistantBasedOrchestrator AddAssistant(BaseAssistant assistant)
    {
        _assistants.Add(assistant);
        assistant.SetOrchestrator(this);
        return this;
    }

    public BaseAssistant GetAssistant(string name)
    {
        return _assistants.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) 
            ?? throw new Exception($"Assistant with name {name} not found");
    }

    public async Task<string> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var kernel = _kernelStore.GetKernel(DefaultModelName);

            //ok I need to get all the functions for all the assistants
            List<KernelFunction> functions = new();
            Dictionary<string, BaseAssistant> assistantMap = new();
            HashSet<string> finalFunctions = new(StringComparer.OrdinalIgnoreCase);

            foreach (var assistant in _assistants)
            {
                foreach (var function in assistant.GetFunctions())
                {
                    functions.Add(function.KernelFunction);
                    assistantMap[function.KernelFunction.Name] = assistant;

                    if (function.IsFinal)
                    {
                        finalFunctions.Add(function.KernelFunction.Name);
                    }
                }
            }

            var settings = new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(functions, autoInvoke: false)
            };

            //ChatMessageContent result = await PerformCallWithChatModel(question, kernel, settings, cancellationToken);
            ChatMessageContent result = await PerformCallWithSimplePromptModel(question, kernel, settings, cancellationToken);

            var response = result.Items.OfType<FunctionCallContent>().SingleOrDefault();
            if (response == null)
            {
                return result.ToString();
            }

            var assistantToCall = assistantMap[response.FunctionName];
            var assistantFunctionCallResult = await assistantToCall.ExecuteFunctionAsync(response.FunctionName, response.Arguments);
            if (finalFunctions.Contains(response.FunctionName))
            {
                return assistantFunctionCallResult;
            }
        }
    }

    private async Task<ChatMessageContent> PerformCallWithSimplePromptModel(string question, Kernel kernel, PromptExecutionSettings settings, CancellationToken cancellationToken)
    {
        StringBuilder prompt = new();
        prompt.AppendLine(
            @"You are an assistant that should answer user question. Analyze facts before deciding what to do next.
If current state can answer user question proceed generating an answer, if not enough information is present, analyze the state to 
determine what tool call next.
When you have all the facts to answer the query, please give the answer without any extra explanation.
The answer can be in a property of an assistant, in that case you can use the GetAssistantProperty function to get the value and return to the user.

FACTS:");

        foreach (var assistant in _assistants)
        {
            var fact = assistant.GetFacts();
            foreach (var f in fact)
            {
                prompt.AppendLine("FACT: " + f);
            }
        }

        prompt.AppendLine("User Question: " + question);

        var functionResult = await kernel.InvokePromptAsync(prompt.ToString(), new(settings), cancellationToken: cancellationToken);
        var content = functionResult.GetValue<ChatMessageContent>()!;
        return content;
    }

    private async Task<ChatMessageContent> PerformCallWithChatModel(string question, Kernel kernel, PromptExecutionSettings settings, CancellationToken cancellationToken)
    {
        ChatHistory chatMessages = new();
        chatMessages.AddSystemMessage(
            @"You are an assistant that should answer user question. Analyze facts before deciding what to do next.
If current state can answer user question proceed generating an answer, if not enough information is present, analyze the state to 
determine what tool call next

FACTS FOLLOW");

        foreach (var assistant in _assistants)
        {
            assistant.AddStateToPrompt(chatMessages);
        }

        chatMessages.AddUserMessage("User Question: " + question);

        var chatEngine = kernel.GetRequiredService<IChatCompletionService>();
        var result = await chatEngine.GetChatMessageContentAsync(
            chatMessages,
            settings,
            cancellationToken: cancellationToken);
        return result;
    }
}
