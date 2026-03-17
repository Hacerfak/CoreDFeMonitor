// src/CoreDFeMonitor.Application/DependencyInjection.cs
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CoreDFeMonitor.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Registra todos os Handlers do MediatR que estão neste projeto
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            return services;
        }
    }
}