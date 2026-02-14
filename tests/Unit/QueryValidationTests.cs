using Application.Common;
using Application.Queries;
using Infrastructure;
using Xunit;

namespace UnitTests;

public sealed class QueryValidationTests
{
    [Fact]
    public async Task GetEmployees_ShouldThrow_WhenPageInvalid()
    {
        var handler = new GetEmployeesQueryHandler(new InMemoryEmployeeRepository());

        await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new GetEmployeesQuery(0, 10)));
    }

    [Fact]
    public async Task GetEmployeeByName_ShouldThrow_WhenNameEmpty()
    {
        var handler = new GetEmployeeByNameQueryHandler(new InMemoryEmployeeRepository());

        await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new GetEmployeeByNameQuery(" ")));
    }
}
