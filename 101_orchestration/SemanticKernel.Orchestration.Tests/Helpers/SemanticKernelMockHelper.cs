using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextGeneration;

namespace SemanticKernel.Orchestration.Tests.Helpers;

public record ServiceMocks(
    MockChatCompletionService ChatCompletionMock,
    MockTextGenerationService TextGenerationMock
);

public static class SemanticKernelMockHelper
{
    public static ServiceMocks AddMockedLLM(
        this IServiceCollection services,
        string? serviceId = null)
    {
        var chatCompletionMock = new MockChatCompletionService();
        var textGenerationMock = new MockTextGenerationService();

        services.AddKeyedSingleton<IChatCompletionService>(serviceId, (_, __) => chatCompletionMock);
        services.AddKeyedSingleton<ITextGenerationService>(serviceId, (_, __) => textGenerationMock);

        return new ServiceMocks(chatCompletionMock, textGenerationMock);
    }
}
