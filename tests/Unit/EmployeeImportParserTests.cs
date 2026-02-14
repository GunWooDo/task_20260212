using Application.Common;
using Infrastructure;
using Xunit;

namespace UnitTests;

public sealed class EmployeeImportParserTests
{
    private readonly EmployeeImportParser _sut = new();

    [Fact]
    public void ParseCsv_ShouldParseRows()
    {
        var csv = "김철수, user1@example.com, 010-1234-5678, 2018.03.07";

        var parsed = _sut.ParseCsv(csv);

        Assert.Single(parsed);
        Assert.Equal("김철수", parsed[0].Name);
    }

    [Fact]
    public void ParseCsv_ShouldIgnoreHeader()
    {
        var csv = "name,email,tel,joined\n김철수, user1@example.com, 010-1234-5678, 2018.03.07";

        var parsed = _sut.ParseCsv(csv);

        Assert.Single(parsed);
        Assert.Equal("김철수", parsed[0].Name);
    }

    [Fact]
    public void ParseCsv_ShouldThrow_WhenColumnsInvalid()
    {
        var csv = "김철수, user1@example.com, 2018.03.07";

        var ex = Assert.Throws<AppValidationException>(() => _sut.ParseCsv(csv));

        Assert.Contains("4 columns", ex.Message);
    }

    [Fact]
    public void ParseJson_ShouldParseRows()
    {
        var json = """
            [
              {"name":"홍길동","email":"user2@example.com","tel":"010-1111-2222","joined":"2019-12-05"}
            ]
            """;

        var parsed = _sut.ParseJson(json);

        Assert.Single(parsed);
        Assert.Equal("홍길동", parsed[0].Name);
    }


    [Fact]
    public void ParseJson_ShouldParseSingleObject()
    {
        var json = """
            {"name":"성춘향","email":"user5@example.com","tel":"010-5555-6666","joined":"2020-05-01"}
            """;

        var parsed = _sut.ParseJson(json);

        Assert.Single(parsed);
        Assert.Equal("성춘향", parsed[0].Name);
    }

    [Fact]
    public void ParseJson_ShouldThrow_WhenPrimitivePayload()
    {
        var ex = Assert.Throws<AppValidationException>(() => _sut.ParseJson("123"));

        Assert.Contains("object or an array", ex.Message);
    }

    [Fact]
    public void ParseJson_ShouldThrow_WhenMalformed()
    {
        var malformed = "[{\"name\":\"홍길동\"";

        var ex = Assert.Throws<AppValidationException>(() => _sut.ParseJson(malformed));

        Assert.Contains("invalid json payload", ex.Message);
    }
}
