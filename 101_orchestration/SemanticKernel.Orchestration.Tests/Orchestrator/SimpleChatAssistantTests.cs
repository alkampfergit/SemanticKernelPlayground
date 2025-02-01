using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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

        IServiceCollection serviceDescriptors = new ServiceCollection();
        var serviceProvider = serviceDescriptors.BuildServiceProvider();
        var kernelStore = new KernelStore(serviceProvider);
        kernelStore.AddKernel("gpt4o", builder, ModelInformation.GPT4O, "default");

        var assistant = new SimpleChatAssistant("gpt4o", kernelStore);

        // Act
        var response = await assistant.SendMessageAsync("Hi there!");

        // Assert
        response.Should().Be("Hello, I'm here to help!");
    }

     [Fact]
    public async Task SimpleChatAssistant_Verify_conversation_handling()
    {
        // Arrange
        var builder = Kernel.CreateBuilder();
        var mocks = builder.Services.AddMockedLLM("gpt4o");

        //Seet a simple mock response
        mocks.ChatCompletionMock.SetMockResponse("Hello, I'm here to help!");

        IServiceCollection serviceDescriptors = new ServiceCollection();
        var serviceProvider = serviceDescriptors.BuildServiceProvider();
        var kernelStore = new KernelStore(serviceProvider);
        kernelStore.AddKernel("gpt4o", builder, ModelInformation.GPT4O, "default");

        var assistant = new SimpleChatAssistant("gpt4o", kernelStore);

        // Act
        var response = await assistant.SendMessageAsync("Hi there!");

        // Assert
        response.Should().Be(@"Hello, I'm here to help!");

        // now go on with the conversation, change the answer of the llm
        mocks.ChatCompletionMock.SetChatMockedResponse("I'm still here to help!");

        response = await assistant.SendMessageAsync("I need help!");

        // Assert
        response.Should().Be(@"Chat history:
user: Hi there!
assistant: Hello, I'm here to help!
user: I need help!
I'm still here to help!");

    }
}
