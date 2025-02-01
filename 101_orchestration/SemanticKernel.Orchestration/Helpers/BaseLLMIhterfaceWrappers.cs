using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

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

        services.AddSingleton<IChatCompletionService>(provider =>
        {
            var inner = provider.GetRequiredKeyedService<IChatCompletionService>(decoratedRegistration.ServiceKey);
            var interceptors = provider.GetServices<IChatInterceptorTool>();
            var wrappers = provider.GetServices<IChatWrappingTool>();

            return new IChatCompletionServiceInterceptor(inner, interceptors, wrappers);
        });

        return builder;
    }

    public static IServiceCollection WithInterceptorTransient<T> (this IServiceCollection services)
        where T : class, IChatInterceptorTool
    {
        services.AddTransient<IChatInterceptorTool, T>();
        return services;
    }

    public static IServiceCollection WithWrapperTransient<T>(this IServiceCollection services)
        where T : class, IChatWrappingTool
    {
        services.AddTransient<IChatWrappingTool, T>();
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
