using System;
using Microsoft.SemanticKernel;

namespace SemanticKernel.Orchestration.Orchestrators;

public class SimpleKernelRouter : IKernelRouter
{
    private readonly string _kernelName;

    public SimpleKernelRouter(string kernelName)
    {
        if (string.IsNullOrEmpty(kernelName))
            throw new ArgumentNullException(nameof(kernelName));
            
        _kernelName = kernelName;
    }

    public string SelectKernel(IConversation conversation)
    {
        return _kernelName;
    }
}
