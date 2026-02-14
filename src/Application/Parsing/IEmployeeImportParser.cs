using Domain;

namespace Application.Parsing;

public interface IEmployeeImportParser
{
    IReadOnlyList<Employee> ParseCsv(string text);
    IReadOnlyList<Employee> ParseJson(string text);
}
