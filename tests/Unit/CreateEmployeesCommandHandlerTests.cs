using Application.Commands;
using Application.Common;
using Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace UnitTests;

public sealed class CreateEmployeesCommandHandlerTests
{
    // ──────────────── 성공 케이스 ────────────────

    [Fact]
    public async Task HandleAsync_ShouldInsertSingleCsvEmployee()
    {
        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), new InMemoryEmployeeRepository(), NullLogger<CreateEmployeesCommandHandler>.Instance);

        var result = await handler.HandleAsync(
            new CreateEmployeesCommand("홍길동, user1@example.com, 010-1234-5678, 2020-01-01", null));

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleAsync_ShouldInsertMultipleCsvEmployees()
    {
        var csv = """
            김철수, kim@example.com, 010-1111-2222, 2018.03.07
            박영희, park@example.com, 010-3333-4444, 2019.06.15
            이순신, lee@example.com, 010-5555-6666, 2020.11.20
            """;

        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), new InMemoryEmployeeRepository(), NullLogger<CreateEmployeesCommandHandler>.Instance);

        var result = await handler.HandleAsync(new CreateEmployeesCommand(csv, null));

        Assert.Equal(3, result);
    }

    [Fact]
    public async Task HandleAsync_ShouldInsertSingleJsonEmployee()
    {
        var json = """
            {"name":"성춘향","email":"sung@example.com","tel":"010-7777-8888","joined":"2021-04-01"}
            """;

        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), new InMemoryEmployeeRepository(), NullLogger<CreateEmployeesCommandHandler>.Instance);

        var result = await handler.HandleAsync(new CreateEmployeesCommand(null, json));

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleAsync_ShouldInsertMultipleJsonEmployees()
    {
        var json = """
            [
              {"name":"강감찬","email":"kang@example.com","tel":"010-1010-2020","joined":"2015-08-10"},
              {"name":"을지문덕","email":"eulji@example.com","tel":"010-3030-4040","joined":"2016-02-28"},
              {"name":"장보고","email":"jang@example.com","tel":"010-5050-6060","joined":"2017-09-01"}
            ]
            """;

        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), new InMemoryEmployeeRepository(), NullLogger<CreateEmployeesCommandHandler>.Instance);

        var result = await handler.HandleAsync(new CreateEmployeesCommand(null, json));

        Assert.Equal(3, result);
    }

    [Fact]
    public async Task HandleAsync_ShouldInsertMultipleBatches_WhenNoDuplicates()
    {
        var repository = new InMemoryEmployeeRepository();
        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), repository, NullLogger<CreateEmployeesCommandHandler>.Instance);

        var result1 = await handler.HandleAsync(
            new CreateEmployeesCommand("홍길동, hong@example.com, 010-1234-5678, 2020-01-01", null));
        var result2 = await handler.HandleAsync(
            new CreateEmployeesCommand("이몽룡, lee@example.com, 010-8888-9999, 2021-05-15", null));

        Assert.Equal(1, result1);
        Assert.Equal(1, result2);
    }

    // ──────────────── 실패 케이스 ────────────────

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenBothCsvAndJsonProvided()
    {
        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), new InMemoryEmployeeRepository(), NullLogger<CreateEmployeesCommandHandler>.Instance);

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new CreateEmployeesCommand("a,b,c,2020-01-01", "[]")));

        Assert.Contains("csv 또는 json 중 하나만 제공해야 합니다", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenNeitherCsvNorJsonProvided()
    {
        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), new InMemoryEmployeeRepository(), NullLogger<CreateEmployeesCommandHandler>.Instance);

        await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new CreateEmployeesCommand(null, null)));
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenBothAreEmptyStrings()
    {
        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), new InMemoryEmployeeRepository(), NullLogger<CreateEmployeesCommandHandler>.Instance);

        await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new CreateEmployeesCommand("", "")));
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenDuplicateEmailInSameRequest()
    {
        var csv = """
            홍길동, user1@example.com, 010-1234-5678, 2020-01-01
            이몽룡, user1@example.com, 010-9999-0000, 2020-01-02
            """;

        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), new InMemoryEmployeeRepository(), NullLogger<CreateEmployeesCommandHandler>.Instance);

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new CreateEmployeesCommand(csv, null)));

        Assert.Contains("요청에 중복된 이메일이 있습니다", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenDuplicateTelInSameRequest()
    {
        var csv = """
            홍길동, hong@example.com, 010-1234-5678, 2020-01-01
            이몽룡, lee@example.com, 010-1234-5678, 2020-01-02
            """;

        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), new InMemoryEmployeeRepository(), NullLogger<CreateEmployeesCommandHandler>.Instance);

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new CreateEmployeesCommand(csv, null)));

        Assert.Contains("요청에 중복된 전화번호가 있습니다", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenDuplicateEmailAgainstExistingData()
    {
        var repository = new InMemoryEmployeeRepository();
        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), repository, NullLogger<CreateEmployeesCommandHandler>.Instance);

        await handler.HandleAsync(
            new CreateEmployeesCommand("홍길동, user1@example.com, 010-1234-5678, 2020-01-01", null));

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(
                new CreateEmployeesCommand("임꺽정, user1@example.com, 010-2222-3333, 2020-01-03", null)));

        Assert.Contains("중복된 직원이 존재합니다", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenDuplicateTelAgainstExistingData()
    {
        var repository = new InMemoryEmployeeRepository();
        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), repository, NullLogger<CreateEmployeesCommandHandler>.Instance);

        await handler.HandleAsync(
            new CreateEmployeesCommand("홍길동, hong@example.com, 010-1234-5678, 2020-01-01", null));

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(
                new CreateEmployeesCommand("임꺽정, lim@example.com, 010-1234-5678, 2020-01-03", null)));

        Assert.Contains("중복된 직원이 존재합니다", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenInvalidEmailFormat()
    {
        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), new InMemoryEmployeeRepository(), NullLogger<CreateEmployeesCommandHandler>.Instance);

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(
                new CreateEmployeesCommand("홍길동, not-an-email, 010-1234-5678, 2020-01-01", null)));

        Assert.Contains("유효하지 않은 이메일 형식입니다", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenInvalidPhoneFormat()
    {
        var handler = new CreateEmployeesCommandHandler(new EmployeeImportParser(), new InMemoryEmployeeRepository(), NullLogger<CreateEmployeesCommandHandler>.Instance);

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(
                new CreateEmployeesCommand("홍길동, hong@example.com, 12345, 2020-01-01", null)));

        Assert.Contains("유효하지 않은 전화번호 형식입니다", ex.Message);
    }
}
