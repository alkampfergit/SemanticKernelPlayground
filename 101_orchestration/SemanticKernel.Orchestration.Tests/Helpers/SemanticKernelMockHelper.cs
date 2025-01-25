using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextGeneration;
using Moq;

namespace SemanticKernel.Orchestration.Tests.Helpers;

public record ServiceMocks(
    Mock<IChatCompletionService> ChatCompletionMock,
    Mock<ITextGenerationService> TextGenerationMock
);

public static class SemanticKernelMockHelper
{
    public static ServiceMocks AddMockedLLM(
        this IServiceCollection services,
        string? serviceId = null)
    {
        var chatCompletionMock = new Mock<IChatCompletionService>();
        var textGenerationMock = new Mock<ITextGenerationService>();

        services.AddKeyedSingleton<IChatCompletionService>(serviceId, (_, __) => chatCompletionMock.Object);
        services.AddKeyedSingleton<ITextGenerationService>(serviceId, (_, __) => textGenerationMock.Object);

        return new ServiceMocks(chatCompletionMock, textGenerationMock);
    }
}
