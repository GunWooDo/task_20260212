using Application;
using Application.Commands;
using Application.Common;
using Application.Queries;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var ex = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

        if (ex is AppValidationException vex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { message = vex.Message });
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { message = "internal server error" });
    });
});

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/api/employee", async (
        int page,
        int pageSize,
        IQueryHandler<GetEmployeesQuery, Domain.PagedResult<Domain.Employee>> handler,
        CancellationToken cancellationToken) =>
    {
        var result = await handler.HandleAsync(new GetEmployeesQuery(page, pageSize), cancellationToken);
        return Results.Ok(result);
    })
    .WithName("GetEmployees")
    .WithSummary("직원 기본 연락처를 페이지 단위로 조회합니다.");

app.MapGet("/api/employee/{name}", async (
        string name,
        IQueryHandler<GetEmployeeByNameQuery, IReadOnlyList<Domain.Employee>> handler,
        CancellationToken cancellationToken) =>
    {
        var result = await handler.HandleAsync(new GetEmployeeByNameQuery(name), cancellationToken);
        return result.Count == 0 ? Results.NotFound() : Results.Ok(result);
    })
    .WithName("GetEmployeesByName")
    .WithSummary("이름으로 직원 연락처를 조회합니다.");

app.MapPost("/api/employee", async (
        HttpRequest request,
        ICommandHandler<CreateEmployeesCommand, int> handler,
        CancellationToken cancellationToken) =>
    {
        var (csv, json) = await ResolvePayloadAsync(request, cancellationToken);
        var inserted = await handler.HandleAsync(new CreateEmployeesCommand(csv, json), cancellationToken);
        return Results.Created("/api/employee", new { inserted });
    })
    .WithName("CreateEmployees")
    .WithSummary("파일 업로드 또는 본문 텍스트로 직원 데이터를 추가합니다.");

app.Run();

static async Task<(string? Csv, string? Json)> ResolvePayloadAsync(HttpRequest request, CancellationToken cancellationToken)
{
    if (request.HasFormContentType)
    {
        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.FirstOrDefault();

        if (file is not null)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var content = (await reader.ReadToEndAsync()).Trim();
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(content))
                throw new AppValidationException("uploaded file is empty.");

            return ext switch
            {
                ".csv" => (content, null),
                ".json" => (null, content),
                _ => throw new AppValidationException("only .csv or .json files are supported.")
            };
        }

        var csv = form.TryGetValue("csv", out var csvValues) ? csvValues.ToString() : null;
        var json = form.TryGetValue("json", out var jsonValues) ? jsonValues.ToString() : null;
        var data = form.TryGetValue("data", out var dataValues) ? dataValues.ToString() : null;

        if (!string.IsNullOrWhiteSpace(csv) || !string.IsNullOrWhiteSpace(json))
            return (Normalize(csv), Normalize(json));

        if (!string.IsNullOrWhiteSpace(data))
            return IsJsonByShape(data) ? (null, Normalize(data)) : (Normalize(data), null);
    }

    using var bodyReader = new StreamReader(request.Body);
    var body = (await bodyReader.ReadToEndAsync()).Trim();

    if (string.IsNullOrWhiteSpace(body))
        throw new AppValidationException("request body is empty.");

    if (IsContentTypeJson(request.ContentType) || IsJsonByShape(body))
        return (null, body);

    return (body, null);
}

static string? Normalize(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return null;

    var trimmed = value.Trim();
    return trimmed.Length == 0 ? null : trimmed;
}

static bool IsJsonByShape(string payload)
{
    var trimmed = payload.TrimStart();
    return trimmed.StartsWith("{") || trimmed.StartsWith("[");
}

static bool IsContentTypeJson(string? contentType)
{
    if (string.IsNullOrWhiteSpace(contentType))
        return false;

    return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)
           || contentType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
}

public partial class Program { }
