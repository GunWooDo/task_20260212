using Application.Common;
using Domain;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

public sealed record GetEmployeeByNameQuery(string Name) : IQuery<IReadOnlyList<Employee>>;

public sealed class GetEmployeeByNameQueryHandler(
    IEmployeeRepository repository,
    ILogger<GetEmployeeByNameQueryHandler> logger)
    : IQueryHandler<GetEmployeeByNameQuery, IReadOnlyList<Employee>>
{
    public async Task<IReadOnlyList<Employee>> HandleAsync(GetEmployeeByNameQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Name))
        {
            logger.LogWarning("Search requested with empty name");
            throw new AppValidationException("이름은 필수입니다.");
        }

        var trimmedName = query.Name.Trim();
        logger.LogInformation("Employee search by name: '{Name}'", trimmedName);

        var result = await repository.FindByNameAsync(trimmedName, cancellationToken);

        logger.LogInformation("Search result for '{Name}': {Count} employee(s) found", trimmedName, result.Count);
        return result;
    }
}
