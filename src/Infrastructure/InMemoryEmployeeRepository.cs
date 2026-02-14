using Application.Common;
using Domain;

namespace Infrastructure;

public sealed class InMemoryEmployeeRepository : IEmployeeRepository
{
    private readonly List<Employee> _employees = [];
    private readonly object _sync = new();

    public Task<int> AddRangeAsync(IEnumerable<Employee> employees, CancellationToken cancellationToken = default)
    {
        var incoming = employees.ToList();

        if (incoming.Count == 0)
            throw new AppValidationException("최소 한 명 이상의 직원이 필요합니다.");

        EnsureNoDuplicateInIncoming(incoming);

        lock (_sync)
        {
            var duplicate = incoming.FirstOrDefault(newEmployee =>
                _employees.Any(existing => IsSameIdentity(existing, newEmployee)));

            if (duplicate is not null)
                throw new AppValidationException($"중복된 직원이 존재합니다: {duplicate.Name}/{duplicate.Email}/{duplicate.Tel}");

            _employees.AddRange(incoming);
        }

        return Task.FromResult(incoming.Count);
    }

    public Task<PagedResult<Employee>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var total = _employees.Count;
            var items = _employees
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult(new PagedResult<Employee>(items, page, pageSize, total));
        }
    }

    public Task<IReadOnlyList<Employee>> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var items = _employees
                .Where(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Email)
                .ToList();

            return Task.FromResult<IReadOnlyList<Employee>>(items);
        }
    }

    private static bool IsSameIdentity(Employee a, Employee b)
    {
        return a.Email.Equals(b.Email, StringComparison.OrdinalIgnoreCase)
               || a.Tel.Equals(b.Tel, StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureNoDuplicateInIncoming(IEnumerable<Employee> incoming)
    {
        var byEmail = incoming
            .GroupBy(x => x.Email, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (byEmail is not null)
            throw new AppValidationException($"요청에 중복된 이메일이 있습니다: {byEmail.Key}");

        var byTel = incoming
            .GroupBy(x => x.Tel, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (byTel is not null)
            throw new AppValidationException($"요청에 중복된 전화번호가 있습니다: {byTel.Key}");
    }
}
