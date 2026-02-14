using Application.Common;
using Domain;

namespace Application.Queries;

public sealed record GetEmployeesQuery(int Page, int PageSize) : IQuery<PagedResult<Employee>>;

public sealed class GetEmployeesQueryHandler(IEmployeeRepository repository)
    : IQueryHandler<GetEmployeesQuery, PagedResult<Employee>>
{
    public Task<PagedResult<Employee>> HandleAsync(GetEmployeesQuery query, CancellationToken cancellationToken = default)
    {
        Validation.EnsurePaging(query.Page, query.PageSize);
        return repository.GetPagedAsync(query.Page, query.PageSize, cancellationToken);
    }
}
