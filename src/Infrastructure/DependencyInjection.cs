using Application.Common;
using Application.Parsing;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options => 
            options.UseSqlite("Data Source=employees.db"));
            
        services.AddScoped<IEmployeeRepository, SqliteEmployeeRepository>();
        services.AddSingleton<IEmployeeImportParser, EmployeeImportParser>();
        services.AddScoped<IDispatcher, Dispatcher>();

        return services;
    }
}
