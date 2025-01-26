using Microsoft.SemanticKernel;
using SemanticKernel.Orchestration.Tests.Helpers;
using Xunit;
using FluentAssertions;
using System.Threading.Tasks;
using SemanticKernel.Orchestration.Orchestrators;
using Microsoft.SemanticKernel.ChatCompletion;
using HandlebarsDotNet;
using System.Linq;

namespace SemanticKernel.Orchestration.Tests;

public class VerifyBasicMockingHelpers
{
    [Fact]
    public void Should_Be_Able_To_Create_Mocked_Kernel()
    {
        // Arrange
        var builder = Kernel.CreateBuilder();
        var mocks = builder.Services.AddMockedLLM("gpt4o");

        // Act
        var kernel = builder.Build();

        // Assert
        kernel.Should().NotBeNull();
        var service = kernel.GetRequiredService<Microsoft.SemanticKernel.TextGeneration.ITextGenerationService>("gpt4o");
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Be_Able_To_Use_Mocked_Kernel_AskAsync()
    {
        // Arrange
        var builder = Kernel.CreateBuilder();
        var mocks = builder.Services.AddMockedLLM("gpt4o");

        var kernel = builder.Build();
        
        // Act
        var prompt = "What is the capital of Italy?";
        var result = await kernel.InvokePromptAsync(prompt);

        // Assert
        result.GetValue<string>().Should().NotBeNullOrEmpty();
        result.GetValue<string>().Should().Be(@"Chat history:
system: What is the capital of Italy?
Dummy response");
    }

       [Fact]
    public async Task Should_Be_Able_To_Use_Mocked_append_Kernel_AskAsync()
    {
        // Arrange
        var builder = Kernel.CreateBuilder();
        var mocks = builder.Services.AddMockedLLM("gpt4o");
        mocks.ChatCompletionMock.SetChatMockedResponse("Rome!!!!!");
        var kernel = builder.Build();
        
        // Act
        var prompt = "What is the capital of Italy?";
        var result = await kernel.InvokePromptAsync(prompt);

        // Assert
        result.GetValue<string>().Should().NotBeNullOrEmpty();
        result.GetValue<string>().Should().Be(@"Chat history:
system: What is the capital of Italy?
Rome!!!!!");
    }

    [Fact]
    public async Task Should_Be_Able_To_Use_Mocked_Kernel_chat_AskAsync()
    {
        // Arrange
        var builder = Kernel.CreateBuilder();
        var mocks = builder.Services.AddMockedLLM("gpt4o");

        var kernel = builder.Build();
        
        // Act
        ChatHistory chatHistory = new();
        chatHistory.AddSystemMessage("System");
        chatHistory.AddUserMessage("What is the capital of Italy?");

        var ccs = kernel.GetRequiredService<IChatCompletionService>();
        var results = await ccs.GetChatMessageContentsAsync(chatHistory);    

        // Assert
        var result = results.Single();
        result.ToString().Should().Be(@"Chat history:
system: System
user: What is the capital of Italy?
Dummy response");
    }

    [Fact]
    public async Task Should_Be_Able_To_Mock_a_response()
    {
        // Arrange
        var builder = Kernel.CreateBuilder();
        var mocks = builder.Services.AddMockedLLM("gpt4o");
        mocks.ChatCompletionMock.SetMockResponse("this is a test");

        var kernel = builder.Build();
        
        // Act
        var prompt = "What is the capital of Italy?";
        var result = await kernel.InvokePromptAsync(prompt);

        // Assert
        result.GetValue<string>().Should().NotBeNullOrEmpty();
        result.GetValue<string>().Should().Be("this is a test");
    }

      [Fact]
    public async Task Should_Be_Able_To_Mock_a_series_of_responses()
    {
        // Arrange
        var builder = Kernel.CreateBuilder();
        var mocks = builder.Services.AddMockedLLM("gpt4o");
        mocks.ChatCompletionMock.SetMockResponse("this is a test", "This is another test");

        var kernel = builder.Build();
        
        // Act
        var prompt = "What is the capital of Italy?";
        var result = await kernel.InvokePromptAsync(prompt);

        // Assert
        result.GetValue<string>().Should().NotBeNullOrEmpty();
        result.GetValue<string>().Should().Be("this is a test");

        // Act invoke again 
        result = await kernel.InvokePromptAsync(prompt);

        // Assert
        result.GetValue<string>().Should().NotBeNullOrEmpty();
        result.GetValue<string>().Should().Be("This is another test");

        // Act invoke again, now responses are ended so we should get the last response
        result = await kernel.InvokePromptAsync(prompt);

        // Assert
        result.GetValue<string>().Should().NotBeNullOrEmpty();
        result.GetValue<string>().Should().Be("This is another test");
    }

    [Fact]
    public async Task Should_Be_Able_To_Use_Mocked_Kernel_From_Store()
    {
        // Arrange
        var builder = Kernel.CreateBuilder();
        var mocks = builder.Services.AddMockedLLM("gpt4o");
        mocks.ChatCompletionMock.SetMockResponse("response from store");

        var kernelStore = new KernelStore();
        kernelStore.AddKernel("gpt4o", builder, ModelInformation.GPT4O);

        // Act
        var kernel = kernelStore.GetKernel("gpt4o");
        var prompt = "What is the capital of Italy?";
        var result = await kernel.InvokePromptAsync(prompt);

        // Assert
        result.GetValue<string>().Should().NotBeNullOrEmpty();
        result.GetValue<string>().Should().Be("response from store");
    }
}
