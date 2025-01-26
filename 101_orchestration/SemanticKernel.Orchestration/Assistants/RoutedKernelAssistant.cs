// using System;
// using System.Linq;
// using System.Text;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.SemanticKernel.ChatCompletion;
// using SemanticKernel.Orchestration.Orchestrators;

// namespace SemanticKernel.Orchestration.Assistants;

// public class RoutedKernelAssistant
// {
//     private readonly string _routerKernelName;
//     private readonly KernelStore _kernelStore;
//     private readonly IConversation _conversation;

//     public RoutedKernelAssistant(
//         string routerKernelName,
//         KernelStore kernelStore,
//         IConversation conversation = null)
//     {
//         _routerKernelName = routerKernelName;
//         _kernelStore = kernelStore;
//         _conversation = conversation ?? new SimpleConversation();
//     }

//     public async Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default)
//     {
//         await _conversation.AddUserMessageAsync(message, cancellationToken);
        
//         // First, get the router's decision
//         var routerKernel = _kernelStore.GetKernel(_routerKernelName);
//         var routerChatCompletion = routerKernel.GetRequiredService<IChatCompletionService>();
        
//         // Create the routing prompt
//         var routingPrompt = CreateRoutingPrompt();
//         await _conversation.AddSystemMessageAsync(routingPrompt, cancellationToken);
        
//         var chatHistory = await _conversation.GetChatHistoryAsync(cancellationToken);
//         var routerResponse = await routerChatCompletion.GetChatMessageContentsAsync(
//             chatHistory, 
//             cancellationToken: cancellationToken);
        
//         var selectedKernel = routerResponse.Single().ToString()!.Trim();
        
//         // Route to the selected kernel
//         var targetKernel = _kernelStore.GetKernel(selectedKernel);
//         var targetChatCompletion = targetKernel.GetRequiredService<IChatCompletionService>();
        
//         var results = await targetChatCompletion.GetChatMessageContentsAsync(
//             chatHistory, 
//             cancellationToken: cancellationToken);
        
//         var result = results.Single();
//         await _conversation.AddAssistantMessageAsync(result, cancellationToken);
        
//         return result.ToString()!;
//     }

//     private string CreateRoutingPrompt()
//     {
//         var sb = new StringBuilder();
//         sb.AppendLine("Based on the user message, select the most appropriate kernel from the following list:");
//         sb.AppendLine();
        
//         foreach (var kernelInfo in _kernelStore.GetAvailableKernels(_routerKernelName))
//         {
//             sb.AppendLine($"- {kernelInfo.Name}: {kernelInfo.Description}");
//         }
        
//         sb.AppendLine();
//         sb.AppendLine("Respond only with the kernel name, nothing else.");
        
//         return sb.ToString();
//     }
// }
