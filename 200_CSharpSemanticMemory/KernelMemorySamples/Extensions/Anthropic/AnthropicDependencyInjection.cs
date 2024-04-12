using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;

namespace SemanticMemory.Extensions.Anthropic
{
    public static class AnthropicDependencyInjection
    {
        public static IKernelMemoryBuilder WithAnthropicTextGeneration(
            this IKernelMemoryBuilder builder,
            AnthropicTextGenerationConfiguration config)
        {
            builder.Services.AddAnthropicTextGeneration(config);
            return builder;
        }

        public static IServiceCollection AddAnthropicTextGeneration(
            this IServiceCollection services,
            AnthropicTextGenerationConfiguration config)
        {
            services.AddSingleton(config);
            return services.AddSingleton<ITextGenerator, AnthropicTextGeneration>();
        }
    }
}
