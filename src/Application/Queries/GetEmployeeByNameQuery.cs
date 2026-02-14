using Application.Common;
using Domain;

namespace Application.Queries;

public sealed record GetEmployeeByNameQuery(string Name) : IQuery<IReadOnlyList<Employee>>;

public sealed class GetEmployeeByNameQueryHandler(IEmployeeRepository repository)
    : IQueryHandler<GetEmployeeByNameQuery, IReadOnlyList<Employee>>
{
    public async Task<IReadOnlyList<Employee>> HandleAsync(GetEmployeeByNameQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Name))
            throw new AppValidationException("name is required.");

        return await repository.FindByNameAsync(query.Name.Trim(), cancellationToken);
    }
}
