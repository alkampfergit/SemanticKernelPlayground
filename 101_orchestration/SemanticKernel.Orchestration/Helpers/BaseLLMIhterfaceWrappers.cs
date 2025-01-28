using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Scrutor;

namespace SemanticKernel.Orchestration.Helpers;

public static class WrapperExtensions
{
    public static IKernelBuilder EnableInterception(this IKernelBuilder builder)
    {
        var services = builder.Services;
        var serviceType = typeof(IChatCompletionService);
        var registeredService = services
            .Where(s => s.ServiceType == serviceType)
            .FirstOrDefault();

        if (registeredService == null)
        {
            throw new Exception("The service IChatCompletionService is not registered");
        }

        var decoratedRegistration = CreateDecoratedService(registeredService);
        services.Add(decoratedRegistration);
        services.Remove(registeredService);

        //ok we have two distinct situation, first one the other service is still not registered
        //or the service is already registered
        services.AddSingleton<IChatCompletionService>(provider =>
        {
            var inner = provider.GetRequiredKeyedService<IChatCompletionService>(decoratedRegistration.ServiceKey);

            return new IChatCompletionServiceInterceptor(inner);
        });

        return builder;
    }

    public static IServiceCollection WithInterceptorTransient<T> (this IServiceCollection services)
        where T : class, IChatInterceptorTool
    {
        services.AddTransient<IChatInterceptorTool, T>();
        return services;
    }

    private static ServiceDescriptor CreateDecoratedService(ServiceDescriptor registeredService)
    {
        var decoratedKey = (registeredService.ServiceKey ?? "default") + "_decorated";
        if (registeredService.IsKeyedService)
        {
            if (registeredService.KeyedImplementationFactory is not null)
            {
                return new ServiceDescriptor(
                    registeredService.ServiceType,
                    decoratedKey,
                    registeredService.KeyedImplementationFactory,
                    registeredService.Lifetime);
            }
            else if (registeredService.KeyedImplementationInstance is not null)
            {
                return new ServiceDescriptor(
                    registeredService.ServiceType,
                    decoratedKey,
                    registeredService.KeyedImplementationInstance);
            }
            else if (registeredService.KeyedImplementationType is not null)
            {
                return new ServiceDescriptor(
                    registeredService.ServiceType,
                    decoratedKey,
                    registeredService.KeyedImplementationType,
                    registeredService.Lifetime);
            }
        }
        else
        {
            if (registeredService.ImplementationFactory is not null)
            {
                return new ServiceDescriptor(
                    registeredService.ServiceType,
                    registeredService.ImplementationFactory,
                    registeredService.Lifetime);
            }
            else if (registeredService.ImplementationInstance is not null)
            {
                return new ServiceDescriptor(
                    registeredService.ServiceType,
                    registeredService.ImplementationInstance);
            }
            else if (registeredService.ImplementationType is not null)
            {
                return new ServiceDescriptor(
                    registeredService.ServiceType,
                    registeredService.ImplementationType,
                    registeredService.Lifetime);
            }
        }

        throw new NotSupportedException("The service descriptor is not supported");
    }
}

public class IChatCompletionServiceInterceptor : IChatCompletionService
{
    private readonly IChatCompletionService _inner;

    public IChatCompletionServiceInterceptor(
        IChatCompletionService inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IReadOnlyDictionary<string, object> Attributes => _inner.Attributes;

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings executionSettings = null,
        Kernel kernel = null,
        CancellationToken cancellationToken = default)
    {
        var counter = InterceptorManager.GetActiveCounter();
        var result = await _inner.GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
        
        if (counter != null)
        {
            await counter.OnChatCompletionAsync(result, chatHistory, executionSettings, kernel, cancellationToken);
        }

        return result;
    }

    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings executionSettings = null,
        Kernel kernel = null,
        CancellationToken cancellationToken = default)
    {
        return _inner.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
    }
}
