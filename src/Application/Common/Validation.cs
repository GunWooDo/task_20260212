using System.Text.RegularExpressions;
using Domain;

namespace Application.Common;

public static partial class Validation
{
    public static void EnsurePaging(int page, int pageSize)
    {
        if (page <= 0 || pageSize <= 0)
        {
            throw new AppValidationException("page와 pageSize는 0보다 커야 합니다.");
        }
    }

    public static Employee NormalizeAndValidate(Employee employee)
    {
        var name = employee.Name.Trim();
        var email = employee.Email.Trim();
        var tel = employee.Tel.Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new AppValidationException("이름은 필수입니다.");

        if (!EmailRegex().IsMatch(email))
            throw new AppValidationException($"유효하지 않은 이메일 형식입니다: {email}");

        if (!PhoneRegex().IsMatch(tel))
            throw new AppValidationException($"유효하지 않은 전화번호 형식입니다: {tel}");

        return employee with { Name = name, Email = email, Tel = tel };
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^(?:\d{10,11}|\d{2,4}-\d{3,4}-\d{4})$")]
    private static partial Regex PhoneRegex();
}
