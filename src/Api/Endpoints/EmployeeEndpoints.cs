using Api;
using Application.Commands;
using Application.Common;
using Application.Queries;

namespace Api.Endpoints;

public static class EmployeeEndpoints
{
    public static void MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employee")
            .WithTags("Employee");

        group.MapGet("/", GetEmployees)
            .WithName("GetEmployees")
            .WithSummary("직원 기본 연락처를 페이지 단위로 조회합니다.");

        group.MapGet("/{name}", GetEmployeesByName)
            .WithName("GetEmployeesByName")
            .WithSummary("이름으로 직원 연락처를 조회합니다.");

        group.MapPost("/", CreateEmployees)
            .DisableAntiforgery()
            .Accepts<Api.Dtos.EmployeeUploadDto>("multipart/form-data", "application/json", "text/plain")
            .WithName("CreateEmployees")
            .WithSummary("파일 업로드 또는 본문 텍스트로 직원 데이터를 추가합니다.");
    }

    private static async Task<IResult> GetEmployees(
        int page,
        int pageSize,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.QueryAsync(new GetEmployeesQuery(page, pageSize), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetEmployeesByName(
        string name,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.QueryAsync(new GetEmployeeByNameQuery(name), cancellationToken);
        return result.Count == 0 ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateEmployees(
        HttpRequest request,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var (csv, json) = await PayloadResolver.ResolvePayloadAsync(request, cancellationToken);
        var inserted = await dispatcher.SendAsync(new CreateEmployeesCommand(csv, json), cancellationToken);
        return Results.Created("/api/employee", new { inserted });
    }
}
