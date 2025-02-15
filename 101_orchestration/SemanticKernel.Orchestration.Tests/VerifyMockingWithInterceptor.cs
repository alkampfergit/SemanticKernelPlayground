using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using SemanticKernel.Orchestration.Helpers;
using SemanticKernel.Orchestration.Tests.Helpers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SemanticKernel.Orchestration.Tests;

public class VerifyMockingWithInterceptor
{
    [Fact]
    public async Task Verify_Interceptor_Can_Mock_Response()
    {
        // Arrange
        var mockWrapper = new Mock<IChatWrappingTool>();
        var mockedResponse = new List<ChatMessageContent>
        {
            new ChatMessageContent(AuthorRole.Assistant, "Mocked response")
        };

        mockWrapper
            .Setup(x => x.OnChatWrappingAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedResponse);

        var builder = Kernel.CreateBuilder();
        //add a mocked llm but in reality we are going to use the mockWrapper
        builder.Services.AddMockedLLM("gpt4o");
        builder.Services.AddSingleton(mockWrapper.Object);
        builder.EnableInterception();

        var kernel = builder.Build();

        // Act
        var response = await kernel.InvokePromptAsync("What is the capital of Italy?");

        // Assert
        response.ToString().Should().Be("Mocked response");
        mockWrapper.Verify(x => x.OnChatWrappingAsync(
            It.IsAny<ChatHistory>(),
            It.IsAny<PromptExecutionSettings>(),
            It.IsAny<Kernel>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
