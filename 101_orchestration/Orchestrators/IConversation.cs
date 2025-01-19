using System;
using System.Collections.Generic;

namespace SemanticKernel.Orchestration.Orchestrators;

public interface IConversation
{
    string CurrentMessage { get; }
    IReadOnlyList<string> History { get; }
    void AddMessage(string message);
}
