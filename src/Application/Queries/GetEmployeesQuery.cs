using Application.Common;
using Domain;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

public sealed record GetEmployeesQuery(int Page, int PageSize) : IQuery<PagedResult<Employee>>;

public sealed class GetEmployeesQueryHandler(
    IEmployeeRepository repository,
    ILogger<GetEmployeesQueryHandler> logger)
    : IQueryHandler<GetEmployeesQuery, PagedResult<Employee>>
{
    public Task<PagedResult<Employee>> HandleAsync(GetEmployeesQuery query, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Employee list query requested (page={Page}, pageSize={PageSize})", query.Page, query.PageSize);

        Validation.EnsurePaging(query.Page, query.PageSize);
        return repository.GetPagedAsync(query.Page, query.PageSize, cancellationToken);
    }
}
