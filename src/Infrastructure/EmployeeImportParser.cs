using System.Globalization;
using System.Text.Json;
using Application.Common;
using Application.Parsing;
using Domain;

namespace Infrastructure;

public sealed class EmployeeImportParser : IEmployeeImportParser
{
    public IReadOnlyList<Employee> ParseCsv(string text)
    {
        var lines = text
            .Replace("\uFEFF", string.Empty)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (lines.Count == 0)
            throw new AppValidationException("CSV 파일이 비어 있습니다.");

        if (IsHeaderLine(lines[0]))
            lines.RemoveAt(0);

        if (lines.Count == 0)
            throw new AppValidationException("CSV 파일에 데이터가 없습니다.");

        var result = new List<Employee>(lines.Count);

        foreach (var line in lines)
        {
            var cols = line.Split(',', StringSplitOptions.TrimEntries);
            if (cols.Length != 4)
                throw new AppValidationException($"CSV 행은 4개의 컬럼을 가져야 합니다: {line}");

            if (!TryParseDate(cols[3], out var joined))
                throw new AppValidationException($"유효하지 않은 입사일 형식입니다: {cols[3]}");

            result.Add(new Employee(cols[0], cols[1], cols[2], joined));
        }

        return result;
    }

    public IReadOnlyList<Employee> ParseJson(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);

            var nodes = doc.RootElement.ValueKind switch
            {
                JsonValueKind.Array => JsonSerializer.Deserialize<List<EmployeeJson>>(doc.RootElement.GetRawText(), JsonOptions),
                JsonValueKind.Object => [JsonSerializer.Deserialize<EmployeeJson>(doc.RootElement.GetRawText(), JsonOptions)!],
                _ => throw new AppValidationException("JSON 데이터는 객체 또는 배열이어야 합니다.")
            };

            if (nodes is null || nodes.Count == 0)
                throw new AppValidationException("JSON 데이터가 비어 있습니다.");

            return nodes.Select(ToEmployee).ToList();
        }
        catch (JsonException)
        {
            throw new AppValidationException("유효하지 않은 JSON 형식입니다.");
        }
    }

    private static Employee ToEmployee(EmployeeJson x)
    {
        if (!TryParseDate(x.Joined, out var joined))
            throw new AppValidationException($"유효하지 않은 입사일 형식입니다: {x.Joined}");

        return new Employee(x.Name ?? string.Empty, x.Email ?? string.Empty, x.Tel ?? string.Empty, joined);
    }

    private static bool IsHeaderLine(string line)
    {
        var values = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (values.Length < 4)
            return false;

        var first = values[0].ToLowerInvariant();
        var second = values[1].ToLowerInvariant();
        var isNameHeader = first is "name" or "이름";
        var isEmailHeader = second is "email" or "mail" or "메일" or "이메일";
        return isNameHeader && isEmailHeader;
    }

    private static bool TryParseDate(string? input, out DateOnly date)
    {
        return DateOnly.TryParseExact(input, ["yyyy.MM.dd", "yyyy-MM-dd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record EmployeeJson(string? Name, string? Email, string? Tel, string? Joined);
}
