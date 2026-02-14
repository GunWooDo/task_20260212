using Application.Common;
using Application.Parsing;
using Domain;
using Microsoft.Extensions.Logging;

namespace Application.Commands;

public sealed record CreateEmployeesCommand(
    string? CsvText,
    string? JsonText) : ICommand<int>;

public sealed class CreateEmployeesCommandHandler(
    IEmployeeImportParser parser,
    IEmployeeRepository repository,
    ILogger<CreateEmployeesCommandHandler> logger) : ICommandHandler<CreateEmployeesCommand, int>
{
    public async Task<int> HandleAsync(CreateEmployeesCommand command, CancellationToken cancellationToken = default)
    {
        var hasCsv = !string.IsNullOrWhiteSpace(command.CsvText);
        var hasJson = !string.IsNullOrWhiteSpace(command.JsonText);

        if (hasCsv == hasJson)
        {
            logger.LogWarning("Invalid request: both csv and json are {Status}", hasCsv ? "provided" : "missing");
            throw new AppValidationException("csv 또는 json 중 하나만 제공해야 합니다.");
        }

        var format = hasCsv ? "CSV" : "JSON";
        logger.LogInformation("Employee creation request received (format: {Format})", format);

        IReadOnlyList<Employee> parsed = hasCsv
            ? parser.ParseCsv(command.CsvText!)
            : parser.ParseJson(command.JsonText!);

        logger.LogInformation("Parsing completed: {Count} employee(s) parsed", parsed.Count);

        var normalized = parsed.Select(Validation.NormalizeAndValidate).ToList();
        var inserted = await repository.AddRangeAsync(normalized, cancellationToken);

        logger.LogInformation("{Count} employee(s) successfully registered", inserted);
        return inserted;
    }
}
