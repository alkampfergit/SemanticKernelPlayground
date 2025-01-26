using System;
using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace SemanticKernel.Orchestration.Tests.Helpers;

[Description("This is a test plugin to manage task")]
public class TestPlugin
{
    private int changeTitleCallCount = 0;

    [KernelFunction]
    [Description("Change the title of a task")]
    public string ChangeTitle(
        [Description("new title for the task")] string newTitle)
    {
        this.changeTitleCallCount++;
        return $"Changed title to {newTitle}";
    }

    public int GetChangeTitleCallCount() => this.changeTitleCallCount;
}
