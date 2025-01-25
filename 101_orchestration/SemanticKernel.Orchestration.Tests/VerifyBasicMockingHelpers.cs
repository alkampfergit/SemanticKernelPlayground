using Microsoft.SemanticKernel;
using SemanticKernel.Orchestration.Tests.Helpers;
using Xunit;
using FluentAssertions;
using System.Threading.Tasks;
using SemanticKernel.Orchestration.Orchestrators;

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
        result.GetValue<string>().Should().Be("Dummy response");
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
