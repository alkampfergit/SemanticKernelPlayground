using Microsoft.SemanticKernel;

namespace SemanticKernel.Orchestration.Orchestrators;

public interface IKernelRouter
{
    string SelectKernel(IConversation conversation);
}
