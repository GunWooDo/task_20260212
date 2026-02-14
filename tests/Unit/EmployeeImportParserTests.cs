using Application.Common;
using Infrastructure;
using Xunit;

namespace UnitTests;

public sealed class EmployeeImportParserTests
{
    private readonly EmployeeImportParser _sut = new();

    // ──────────────── CSV 성공 케이스 ────────────────

    [Fact]
    public void ParseCsv_ShouldParseMultipleRows()
    {
        var csv = """
            김철수, user1@example.com, 010-1234-5678, 2018.03.07
            박영희, user2@example.com, 010-8765-4321, 2021.04.28
            이순신, user3@example.com, 01012345678, 2015.01.15
            """;

        var parsed = _sut.ParseCsv(csv);

        Assert.Equal(3, parsed.Count);
        Assert.Equal("김철수", parsed[0].Name);
        Assert.Equal("user1@example.com", parsed[0].Email);
        Assert.Equal("010-1234-5678", parsed[0].Tel);
        Assert.Equal(new DateOnly(2018, 3, 7), parsed[0].Joined);

        Assert.Equal("박영희", parsed[1].Name);
        Assert.Equal("user2@example.com", parsed[1].Email);

        Assert.Equal("이순신", parsed[2].Name);
        Assert.Equal("01012345678", parsed[2].Tel);
    }

    [Fact]
    public void ParseCsv_ShouldIgnoreKoreanHeader()
    {
        var csv = "이름,이메일,전화번호,입사일\n김철수, user1@example.com, 010-1234-5678, 2018.03.07";

        var parsed = _sut.ParseCsv(csv);

        Assert.Single(parsed);
        Assert.Equal("김철수", parsed[0].Name);
    }

    [Fact]
    public void ParseCsv_ShouldIgnoreEnglishHeader()
    {
        var csv = "name,email,tel,joined\n홍길동, hong@example.com, 010-9999-8888, 2020.06.15";

        var parsed = _sut.ParseCsv(csv);

        Assert.Single(parsed);
        Assert.Equal("홍길동", parsed[0].Name);
    }

    [Fact]
    public void ParseCsv_ShouldHandleBOM()
    {
        var csv = "\uFEFF김철수, user1@example.com, 010-1234-5678, 2018.03.07";

        var parsed = _sut.ParseCsv(csv);

        Assert.Single(parsed);
        Assert.Equal("김철수", parsed[0].Name);
    }

    [Fact]
    public void ParseCsv_ShouldHandleDashDateFormat()
    {
        var csv = "강감찬, kang@example.com, 010-7777-6666, 2019-11-25";

        var parsed = _sut.ParseCsv(csv);

        Assert.Single(parsed);
        Assert.Equal(new DateOnly(2019, 11, 25), parsed[0].Joined);
    }

    [Fact]
    public void ParseCsv_ShouldHandlePhoneWithoutDashes()
    {
        var csv = "최무선, choi@example.com, 01055556666, 2022.08.30";

        var parsed = _sut.ParseCsv(csv);

        Assert.Single(parsed);
        Assert.Equal("01055556666", parsed[0].Tel);
    }

    // ──────────────── CSV 실패 케이스 ────────────────

    [Fact]
    public void ParseCsv_ShouldThrow_WhenColumnsAreTooFew()
    {
        var csv = "김철수, user1@example.com, 2018.03.07";

        var ex = Assert.Throws<AppValidationException>(() => _sut.ParseCsv(csv));

        Assert.Contains("CSV 행은 4개의 컬럼을 가져야 합니다", ex.Message);
    }

    [Fact]
    public void ParseCsv_ShouldThrow_WhenColumnsAreTooMany()
    {
        var csv = "김철수, user1@example.com, 010-1234-5678, 2018.03.07, 추가데이터";

        var ex = Assert.Throws<AppValidationException>(() => _sut.ParseCsv(csv));

        Assert.Contains("CSV 행은 4개의 컬럼을 가져야 합니다", ex.Message);
    }

    [Fact]
    public void ParseCsv_ShouldThrow_WhenDateFormatInvalid()
    {
        var csv = "김철수, user1@example.com, 010-1234-5678, 03/07/2018";

        var ex = Assert.Throws<AppValidationException>(() => _sut.ParseCsv(csv));

        Assert.Contains("유효하지 않은 입사일 형식입니다", ex.Message);
    }

    [Fact]
    public void ParseCsv_ShouldThrow_WhenEmpty()
    {
        var ex = Assert.Throws<AppValidationException>(() => _sut.ParseCsv(""));

        Assert.Contains("비어", ex.Message);
    }

    [Fact]
    public void ParseCsv_ShouldThrow_WhenOnlyHeaderPresent()
    {
        var csv = "name,email,tel,joined";

        var ex = Assert.Throws<AppValidationException>(() => _sut.ParseCsv(csv));

        Assert.Contains("데이터가 없습니다", ex.Message);
    }

    // ──────────────── JSON 성공 케이스 ────────────────

    [Fact]
    public void ParseJson_ShouldParseMultipleObjects()
    {
        var json = """
            [
              {"name":"홍길동","email":"hong@example.com","tel":"010-1111-2222","joined":"2019-12-05"},
              {"name":"이몽룡","email":"lee@example.com","tel":"010-3333-4444","joined":"2020-03-15"},
              {"name":"성춘향","email":"sung@example.com","tel":"010-5555-6666","joined":"2021-07-20"}
            ]
            """;

        var parsed = _sut.ParseJson(json);

        Assert.Equal(3, parsed.Count);
        Assert.Equal("홍길동", parsed[0].Name);
        Assert.Equal("hong@example.com", parsed[0].Email);
        Assert.Equal("010-1111-2222", parsed[0].Tel);
        Assert.Equal(new DateOnly(2019, 12, 5), parsed[0].Joined);

        Assert.Equal("이몽룡", parsed[1].Name);
        Assert.Equal("010-3333-4444", parsed[1].Tel);

        Assert.Equal("성춘향", parsed[2].Name);
        Assert.Equal(new DateOnly(2021, 7, 20), parsed[2].Joined);
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
        Assert.Equal("user5@example.com", parsed[0].Email);
    }

    [Fact]
    public void ParseJson_ShouldHandleCaseInsensitivePropertyNames()
    {
        var json = """
            {"Name":"변사또","Email":"byun@example.com","Tel":"010-7777-8888","Joined":"2023-01-10"}
            """;

        var parsed = _sut.ParseJson(json);

        Assert.Single(parsed);
        Assert.Equal("변사또", parsed[0].Name);
        Assert.Equal("byun@example.com", parsed[0].Email);
    }

    [Fact]
    public void ParseJson_ShouldHandleDotDateFormat()
    {
        var json = """
            {"name":"장보고","email":"jang@example.com","tel":"010-1212-3434","joined":"2017.09.01"}
            """;

        var parsed = _sut.ParseJson(json);

        Assert.Single(parsed);
        Assert.Equal(new DateOnly(2017, 9, 1), parsed[0].Joined);
    }

    // ──────────────── JSON 실패 케이스 ────────────────

    [Fact]
    public void ParseJson_ShouldThrow_WhenPrimitivePayload()
    {
        var ex = Assert.Throws<AppValidationException>(() => _sut.ParseJson("123"));

        Assert.Contains("JSON 데이터는 객체 또는 배열이어야 합니다", ex.Message);
    }

    [Fact]
    public void ParseJson_ShouldThrow_WhenStringPayload()
    {
        var ex = Assert.Throws<AppValidationException>(() => _sut.ParseJson("\"hello\""));

        Assert.Contains("JSON 데이터는 객체 또는 배열이어야 합니다", ex.Message);
    }

    [Fact]
    public void ParseJson_ShouldThrow_WhenMalformedJson()
    {
        var malformed = "[{\"name\":\"홍길동\"";

        var ex = Assert.Throws<AppValidationException>(() => _sut.ParseJson(malformed));

        Assert.Contains("유효하지 않은 JSON 형식입니다", ex.Message);
    }

    [Fact]
    public void ParseJson_ShouldThrow_WhenEmptyArray()
    {
        var ex = Assert.Throws<AppValidationException>(() => _sut.ParseJson("[]"));

        Assert.Contains("비어", ex.Message);
    }

    [Fact]
    public void ParseJson_ShouldThrow_WhenInvalidDateInJson()
    {
        var json = """
            {"name":"오류자","email":"err@example.com","tel":"010-0000-0000","joined":"2020/13/45"}
            """;

        var ex = Assert.Throws<AppValidationException>(() => _sut.ParseJson(json));

        Assert.Contains("유효하지 않은 입사일 형식입니다", ex.Message);
    }
}
