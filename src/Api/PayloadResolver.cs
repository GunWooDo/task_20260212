using System.Text.RegularExpressions;
using Application.Common;

namespace Api;

public static class PayloadResolver
{
    public static async Task<(string? Csv, string? Json)> ResolvePayloadAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.FirstOrDefault();

            if (file is not null && file.Length > 0 && !string.IsNullOrWhiteSpace(file.FileName))
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var content = (await reader.ReadToEndAsync()).Trim();
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(content))
                    throw new AppValidationException("업로드된 파일이 비어 있습니다.");

                return ext switch
                {
                    ".csv" => (content, null),
                    ".json" => (null, content),
                    _ => throw new AppValidationException(".csv 또는 .json 파일만 지원됩니다.")
                };
            }

            var csv = form.TryGetValue("csv", out var csvValues) ? csvValues.ToString() : null;
            var json = form.TryGetValue("json", out var jsonValues) ? jsonValues.ToString() : null;
            var data = form.TryGetValue("data", out var dataValues) ? dataValues.ToString() : null;

            data = RestoreCsvRowBreaksIfNeeded(data);

            if (!string.IsNullOrWhiteSpace(csv) || !string.IsNullOrWhiteSpace(json))
                return (Normalize(csv), Normalize(json));

            if (!string.IsNullOrWhiteSpace(data))
                return IsJsonByShape(data) ? (null, Normalize(data)) : (Normalize(data), null);
        }

        using var bodyReader = new StreamReader(request.Body);
        var body = (await bodyReader.ReadToEndAsync()).Trim();

        if (string.IsNullOrWhiteSpace(body))
            throw new AppValidationException("요청 본문이 비어 있습니다.");

        if (IsContentTypeJson(request.ContentType) || IsJsonByShape(body))
            return (null, body);

        return (body, null);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string? GetJoinedValues(IFormCollection form, string key)
    {
        if (!form.TryGetValue(key, out var values) || values.Count == 0)
            return null;

        // StringValues.ToString() joins with comma, which breaks CSV rows.
        // We join with newline instead.
        return string.Join("\n", values.Select(v => v?.Trim()).Where(v => !string.IsNullOrEmpty(v)));
    }

    private static string? RestoreCsvRowBreaksIfNeeded(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;

        var trimmed = s.Trim();
        if (IsJsonByShape(trimmed)) return trimmed;

        // 이미 줄바꿈이 있으면 OK
        if (trimmed.Contains('\n')) return trimmed;

        // 날짜 패턴이 2번 이상이면 "한 줄로 뭉개진 CSV" 가능성이 큼
        var matches = Regex.Matches(trimmed, @"\d{4}\.\d{2}\.\d{2}");
        if (matches.Count <= 1) return trimmed;

        // 날짜 뒤 + 공백 + 다음 토큰(이름) 형태를 줄바꿈으로 바꿔줌
        // 예) "2017.01.07 우건건," -> "2017.01.07\n우건건,"
        return Regex.Replace(trimmed, @"(\d{4}\.\d{2}\.\d{2})\s+(?=\S)", "$1\n");
    }

    private static bool IsJsonByShape(string payload)
    {
        var trimmed = payload.TrimStart();
        return trimmed.StartsWith("{") || trimmed.StartsWith("[");
    }

    private static bool IsContentTypeJson(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)
               || contentType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }
}
