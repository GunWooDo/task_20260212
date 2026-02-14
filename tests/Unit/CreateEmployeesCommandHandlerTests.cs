using Application.Commands;
using Application.Common;
using Infrastructure;
using Xunit;

namespace UnitTests;

public sealed class CreateEmployeesCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenBothCsvAndJsonProvided()
    {
        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), new InMemoryEmployeeRepository());

        await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new CreateEmployeesCommand("a,b,c,2020-01-01", "[]")));
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenDuplicateEmailInSameRequest()
    {
        var csv = """
            홍길동, user1@example.com, 010-1234-5678, 2020-01-01
            이몽룡, user1@example.com, 010-9999-0000, 2020-01-02
            """;

        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), new InMemoryEmployeeRepository());

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new CreateEmployeesCommand(csv, null)));

        Assert.Contains("duplicate email", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenDuplicateAgainstExistingData()
    {
        var repository = new InMemoryEmployeeRepository();
        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), repository);

        await handler.HandleAsync(new CreateEmployeesCommand("홍길동, user1@example.com, 010-1234-5678, 2020-01-01", null));

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new CreateEmployeesCommand("임꺽정, user1@example.com, 010-2222-3333, 2020-01-03", null)));

        Assert.Contains("duplicate employee exists", ex.Message);
    }
}
