using System;
using Microsoft.SemanticKernel;
using SemanticKernel.Orchestration.Tests.Helpers;
using Xunit;

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
        Assert.NotNull(kernel);
        var service = kernel.GetRequiredService<Microsoft.SemanticKernel.TextGeneration.ITextGenerationService>("gpt4o");
        Assert.NotNull(service);
    }
}
