using System.Reflection;
using Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        ServicesFromAssembly(services, assembly);

        return services;
    }

    private static void ServicesFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var types = assembly.GetTypes();

        foreach (var type in types)
        {
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            foreach (var @interface in type.GetInterfaces())
            {
                if (!@interface.IsGenericType)
                {
                    continue;
                }

                var definition = @interface.GetGenericTypeDefinition();

                if (definition == typeof(ICommandHandler<,>) || definition == typeof(IQueryHandler<,>))
                {
                    services.AddScoped(@interface, type);
                }
            }
        }
    }
}
