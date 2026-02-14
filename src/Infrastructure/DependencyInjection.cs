using Application.Common;
using Application.Parsing;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEmployeeRepository, InMemoryEmployeeRepository>();
        services.AddSingleton<IEmployeeImportParser, EmployeeImportParser>();
        services.AddScoped<IDispatcher, Dispatcher>();

        return services;
    }
}
