using Application.Common;
using Application.Parsing;
using Domain;

namespace Application.Commands;

public sealed record CreateEmployeesCommand(
    string? CsvText,
    string? JsonText) : ICommand<int>;

public sealed class CreateEmployeesCommandHandler(
    IEmployeeImportParser parser,
    IEmployeeRepository repository) : ICommandHandler<CreateEmployeesCommand, int>
{
    public async Task<int> HandleAsync(CreateEmployeesCommand command, CancellationToken cancellationToken = default)
    {
        var hasCsv = !string.IsNullOrWhiteSpace(command.CsvText);
        var hasJson = !string.IsNullOrWhiteSpace(command.JsonText);

        if (hasCsv == hasJson)
        {
            throw new AppValidationException("csv 또는 json 중 하나만 제공해야 합니다.");
        }

        IReadOnlyList<Employee> parsed = hasCsv
            ? parser.ParseCsv(command.CsvText!)
            : parser.ParseJson(command.JsonText!);

        var normalized = parsed.Select(Validation.NormalizeAndValidate).ToList();
        return await repository.AddRangeAsync(normalized, cancellationToken);
    }
}
