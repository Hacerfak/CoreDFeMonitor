// src/CoreDFeMonitor.Application/DependencyInjection.cs
using CoreDFeMonitor.Application.Mediator;
using CoreDFeMonitor.Core.Mediator;
using CoreDFeMonitor.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CoreDFeMonitor.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // 1. Registra o nosso Mediator Nativo
            services.AddSingleton<IMediator, NativeMediator>();
            services.AddSingleton<ZeusBackgroundService>();
            services.AddSingleton<ISyncStatusMonitor, SyncStatusMonitor>();

            // 2. Escaneia o projeto atual buscando todas as classes que implementam IRequestHandler
            var assembly = Assembly.GetExecutingAssembly();

            var handlers = assembly.GetTypes()
                .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
                .ToList();

            // 3. Registra cada um deles no contêiner do .NET
            foreach (var handler in handlers)
            {
                var interfaceType = handler.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
                services.AddTransient(interfaceType, handler);
            }

            return services;
        }
    }
}