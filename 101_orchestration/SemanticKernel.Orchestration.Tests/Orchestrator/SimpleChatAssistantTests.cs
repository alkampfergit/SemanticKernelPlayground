using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.SemanticKernel;
using SemanticKernel.Orchestration.Assistants;
using SemanticKernel.Orchestration.Orchestrators;
using SemanticKernel.Orchestration.Tests.Helpers;
using Xunit;

namespace SemanticKernel.Orchestration.Tests.Orchestrator;

public class SimpleChatAssistantTests
{
    [Fact]
    public async Task SimpleChatAssistant_ShouldReturnExpectedResponse()
    {
        // Arrange
        var builder = Kernel.CreateBuilder();
        var mocks = builder.Services.AddMockedLLM("gpt4o");
        mocks.ChatCompletionMock.SetMockResponse("Hello, I'm here to help!");

        var kernelStore = new KernelStore();
        kernelStore.AddKernel("gpt4o", builder, ModelInformation.GPT4O);

        var assistant = new SimpleChatAssistant("gpt4o", kernelStore);

        // Act
        var response = await assistant.SendMessageAsync("Hi there!");

        // Assert
        response.Should().Be("Hello, I'm here to help!");
    }
}
