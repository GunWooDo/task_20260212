using Application.Common;
using Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class SqliteEmployeeRepository(AppDbContext dbContext) : IEmployeeRepository
{
    public async Task<int> AddRangeAsync(IEnumerable<Employee> employees, CancellationToken cancellationToken = default)
    {
        var incoming = employees.ToList();
        if (incoming.Count == 0)
            throw new AppValidationException("최소 한 명 이상의 직원이 필요합니다.");

        // 1. 요청 데이터 내에서의 중복 검사
        EnsureNoDuplicateInIncoming(incoming);

        // 2. 데이터베이스 내에서의 중복 검사
        // 단순함을 위해, 요청된 이메일이나 전화번호가 하나라도 존재하는지 확인합니다.
        // (대량 데이터의 경우 더 효율적인 방법이 필요할 수 있습니다)
        
        var emails = incoming.Select(x => x.Email).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tels = incoming.Select(x => x.Tel).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existing = await dbContext.Employees
            .Where(e => emails.Contains(e.Email) || tels.Contains(e.Tel))
            .ToListAsync(cancellationToken);

        if (existing.Count != 0)
        {
            var duplicate = existing.First();
            throw new AppValidationException($"중복된 직원이 존재합니다: {duplicate.Name}/{duplicate.Email}/{duplicate.Tel}");
        }

        await dbContext.Employees.AddRangeAsync(incoming, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return incoming.Count;
    }

    public async Task<PagedResult<Employee>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Employees
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Email);

        var total = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Employee>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<Employee>> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await dbContext.Employees
            .AsNoTracking()
            .Where(x => x.Name == name) // SQLite는 기본적으로 대소문자를 구분하지 않지만, 설정에 따라 다를 수 있습니다.
            .OrderBy(x => x.Email)
            .ToListAsync(cancellationToken);
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
