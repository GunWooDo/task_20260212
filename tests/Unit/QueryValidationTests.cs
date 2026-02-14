using Application.Commands;
using Application.Common;
using Application.Queries;
using Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace UnitTests;

public sealed class QueryValidationTests
{
    // ──────────────── GetEmployees 성공 케이스 ────────────────

    [Fact]
    public async Task GetEmployees_ShouldReturnEmpty_WhenNoData()
    {
        var handler = new GetEmployeesQueryHandler(new InMemoryEmployeeRepository(), NullLogger<GetEmployeesQueryHandler>.Instance);

        var result = await handler.HandleAsync(new GetEmployeesQuery(1, 10));

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetEmployees_ShouldReturnPaged_WithCorrectMetadata()
    {
        var repository = new InMemoryEmployeeRepository();
        var cmdHandler = new CreateEmployeesCommandHandler(
            new EmployeeImportParser(),
            repository,
            NullLogger<CreateEmployeesCommandHandler>.Instance);

        var csv = """
            김철수, kim@example.com, 010-1111-2222, 2018.03.07
            박영희, park@example.com, 010-3333-4444, 2019.06.15
            이순신, lee@example.com, 010-5555-6666, 2020.11.20
            강감찬, kang@example.com, 010-7777-8888, 2015.01.15
            을지문덕, eulji@example.com, 010-9999-0000, 2016.02.28
            """;
        await cmdHandler.HandleAsync(new Application.Commands.CreateEmployeesCommand(csv, null));

        var queryHandler = new GetEmployeesQueryHandler(repository, NullLogger<GetEmployeesQueryHandler>.Instance);

        var page1 = await queryHandler.HandleAsync(new GetEmployeesQuery(1, 2));

        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(1, page1.Page);
        Assert.Equal(2, page1.PageSize);
    }

    // ──────────────── GetEmployees 실패 케이스 ────────────────

    [Fact]
    public async Task GetEmployees_ShouldThrow_WhenPageIsZero()
    {
        var handler = new GetEmployeesQueryHandler(new InMemoryEmployeeRepository(), NullLogger<GetEmployeesQueryHandler>.Instance);

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new GetEmployeesQuery(0, 10)));

        Assert.Contains("page와 pageSize는 0보다 커야 합니다", ex.Message);
    }

    [Fact]
    public async Task GetEmployees_ShouldThrow_WhenPageIsNegative()
    {
        var handler = new GetEmployeesQueryHandler(new InMemoryEmployeeRepository(), NullLogger<GetEmployeesQueryHandler>.Instance);

        await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new GetEmployeesQuery(-1, 10)));
    }

    [Fact]
    public async Task GetEmployees_ShouldThrow_WhenPageSizeIsZero()
    {
        var handler = new GetEmployeesQueryHandler(new InMemoryEmployeeRepository(), NullLogger<GetEmployeesQueryHandler>.Instance);

        await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new GetEmployeesQuery(1, 0)));
    }

    [Fact]
    public async Task GetEmployees_ShouldThrow_WhenPageSizeIsNegative()
    {
        var handler = new GetEmployeesQueryHandler(new InMemoryEmployeeRepository(), NullLogger<GetEmployeesQueryHandler>.Instance);

        await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new GetEmployeesQuery(1, -5)));
    }

    // ──────────────── GetEmployeeByName 성공 케이스 ────────────────

    [Fact]
    public async Task GetEmployeeByName_ShouldReturnEmpty_WhenNotFound()
    {
        var handler = new GetEmployeeByNameQueryHandler(new InMemoryEmployeeRepository(), NullLogger<GetEmployeeByNameQueryHandler>.Instance);

        var result = await handler.HandleAsync(new GetEmployeeByNameQuery("존재하지않음"));

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetEmployeeByName_ShouldReturnMatchingEmployees()
    {
        var repository = new InMemoryEmployeeRepository();
        var cmdHandler = new CreateEmployeesCommandHandler(
            new EmployeeImportParser(),
            repository,
            NullLogger<CreateEmployeesCommandHandler>.Instance);

        var csv = """
            홍길동, hong1@example.com, 010-1111-2222, 2020-01-01
            홍길동, hong2@example.com, 010-3333-4444, 2021-05-15
            이몽룡, lee@example.com, 010-5555-6666, 2022-03-10
            """;
        await cmdHandler.HandleAsync(new Application.Commands.CreateEmployeesCommand(csv, null));

        var queryHandler = new GetEmployeeByNameQueryHandler(repository, NullLogger<GetEmployeeByNameQueryHandler>.Instance);

        var result = await queryHandler.HandleAsync(new GetEmployeeByNameQuery("홍길동"));

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal("홍길동", e.Name));
    }

    // ──────────────── GetEmployeeByName 실패 케이스 ────────────────

    [Fact]
    public async Task GetEmployeeByName_ShouldThrow_WhenNameEmpty()
    {
        var handler = new GetEmployeeByNameQueryHandler(new InMemoryEmployeeRepository(), NullLogger<GetEmployeeByNameQueryHandler>.Instance);

        await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new GetEmployeeByNameQuery(" ")));
    }

    [Fact]
    public async Task GetEmployeeByName_ShouldThrow_WhenNameIsWhitespaceOnly()
    {
        var handler = new GetEmployeeByNameQueryHandler(new InMemoryEmployeeRepository(), NullLogger<GetEmployeeByNameQueryHandler>.Instance);

        await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.HandleAsync(new GetEmployeeByNameQuery("   ")));
    }
}
