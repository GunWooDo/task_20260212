using Domain;

namespace Application.Common;

public interface IEmployeeRepository
{
    Task<int> AddRangeAsync(IEnumerable<Employee> employees, CancellationToken cancellationToken = default);
    Task<PagedResult<Employee>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> FindByNameAsync(string name, CancellationToken cancellationToken = default);
}
