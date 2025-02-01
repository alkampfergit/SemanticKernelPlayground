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

public class TokenLimitedConversationTests
{
    [Fact]
    public async Task TokenLimitedConversation_Basic_Conversation_Works()
    {
        // Arrange
        var builder = Kernel.CreateBuilder();
        var mocks = builder.Services.AddMockedLLM("gpt4o");
        mocks.ChatCompletionMock.SetMockResponse("Hello, I'm here to help!");
        
        IServiceCollection serviceDescriptors = new ServiceCollection();
        var serviceProvider = serviceDescriptors.BuildServiceProvider();
        var kernelStore = new KernelStore(serviceProvider);
        kernelStore.AddKernel("gpt4o", builder, ModelInformation.GPT4O, "default");

        var conversation = new TokenLimitedConversation(kernelStore, "gpt4o", 2000);
        var assistant = new SimpleChatAssistant("gpt4o", kernelStore, conversation);

        // Act
        var response = await assistant.SendMessageAsync("Hi there!");

        // Assert
        response.Should().Be("Hello, I'm here to help!");
    }

    [Fact]
    public async Task TokenLimitedConversation_Should_Summarize_When_Token_Limit_Exceeded()
    {
        // Arrange
        var builder = Kernel.CreateBuilder();
        var mocks = builder.Services.AddMockedLLM("gpt4o");
        
        // Set up mock for normal responses
        mocks.ChatCompletionMock.SetMockResponse(
            "Here is a very long response that will consume tokens",
            "this is summary");

        IServiceCollection serviceDescriptors = new ServiceCollection();
        var serviceProvider = serviceDescriptors.BuildServiceProvider();
        var kernelStore = new KernelStore(serviceProvider);
        kernelStore.AddKernel("gpt4o", builder, ModelInformation.GPT4O, "default");

        // Set a very low token limit to force summarization
        var conversation = new TokenLimitedConversation(kernelStore, "gpt4o", 10);
        var assistant = new SimpleChatAssistant("gpt4o", kernelStore, conversation);

        // Act
        // First message to establish initial conversation
        var response = await assistant.SendMessageAsync("Hi there!");
        //assert the response is mocked 
        response.Should().Be("Here is a very long response that will consume tokens");

        //now setup to dump all chat mode
        mocks.ChatCompletionMock.SetChatMockedResponse("this is third response");

        response = await assistant.SendMessageAsync("Hi there this is the second question!");
        //assert the response is the summarization
        response.Should().Be(@"Chat history:
system: Conversation so far:
this is summary


user: Hi there this is the second question!
this is third response");
    }
}
