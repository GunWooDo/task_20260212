using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Integration;

public sealed class EmployeeApiIntegrationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName;

    public EmployeeApiIntegrationTests()
    {
        _dbName = $"test_{Guid.NewGuid()}.db";
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<Infrastructure.Persistence.AppDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<Infrastructure.Persistence.AppDbContext>(options =>
                    {
                        options.UseSqlite($"Data Source={_dbName}");
                    });
                });
            });
    }

    public void Dispose()
    {
        _factory.Dispose();
        if (File.Exists(_dbName))
        {
            try { File.Delete(_dbName); } catch { }
        }
    }

    // ──────────────── POST 성공 케이스 ────────────────

    [Fact]
    public async Task PostJsonBody_SingleEmployee_ReturnsCreated()
    {
        using var client = _factory.CreateClient();

        var payload = """
                      {"name":"홍길동","email":"user10@example.com","tel":"010-1111-2222","joined":"2020-05-01"}
                      """;

        using var content = new StringContent(payload);
        content.Headers.ContentType = new("application/json");

        var post = await client.PostAsync("/api/employee", content);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
    }

    [Fact]
    public async Task PostJsonBody_MultipleEmployees_ReturnsCreated()
    {
        using var client = _factory.CreateClient();

        var payload = """
                      [
                        {"name":"김철수","email":"kim@example.com","tel":"010-2222-3333","joined":"2018-03-07"},
                        {"name":"박영희","email":"park@example.com","tel":"010-4444-5555","joined":"2019-06-15"},
                        {"name":"이순신","email":"lee@example.com","tel":"010-6666-7777","joined":"2020-11-20"}
                      ]
                      """;

        using var content = new StringContent(payload);
        content.Headers.ContentType = new("application/json");

        var post = await client.PostAsync("/api/employee", content);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var body = await post.Content.ReadAsStringAsync();
        Assert.Contains("3", body);
    }

    [Fact]
    public async Task PostMultipartCsv_WithMultipleRows_ReturnsCreated()
    {
        using var client = _factory.CreateClient();

        const string csv = "name,email,tel,joined\n김철수,user11@example.com,01012341234,2018.03.07\n박영희,user12@example.com,01056785678,2019.06.15";

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(csv), "file", "employees.csv");

        var post = await client.PostAsync("/api/employee", form);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var page = await client.GetFromJsonAsync<PagedResponse>("/api/employee?page=1&pageSize=10");
        Assert.NotNull(page);
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task PostMultipartJson_WithFile_ReturnsCreated()
    {
        using var client = _factory.CreateClient();

        const string json = """[{"name":"장보고","email":"jang@example.com","tel":"010-7070-8080","joined":"2017-09-01"}]""";

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(json), "file", "employees.json");

        var post = await client.PostAsync("/api/employee", form);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
    }

    [Fact]
    public async Task PostFormFieldData_WithJsonPayload_ReturnsCreated()
    {
        using var client = _factory.CreateClient();

        const string json = """
                            {"name":"성춘향","email":"user13@example.com","tel":"010-2222-3333","joined":"2021-11-30"}
                            """;

        using var form = new MultipartFormDataContent
        {
            { new StringContent(json), "data" }
        };

        var response = await client.PostAsync("/api/employee", form);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostFormFieldCsv_WithCsvField_ReturnsCreated()
    {
        using var client = _factory.CreateClient();

        const string csv = "변사또, byun@example.com, 010-8888-9999, 2022.01.10";

        using var form = new MultipartFormDataContent
        {
            { new StringContent(csv), "csv" }
        };

        var response = await client.PostAsync("/api/employee", form);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ──────────────── GET 조회 성공 케이스 ────────────────

    [Fact]
    public async Task PostThenGetByName_ReturnsInsertedEmployeeDetails()
    {
        using var client = _factory.CreateClient();

        var payload = """
                      {"name":"홍길동","email":"get_test@example.com","tel":"010-1010-2020","joined":"2020-05-01"}
                      """;

        using var content = new StringContent(payload);
        content.Headers.ContentType = new("application/json");

        await client.PostAsync("/api/employee", content);

        var get = await client.GetAsync("/api/employee/홍길동");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var body = await get.Content.ReadAsStringAsync();
        Assert.Contains("홍길동", body);
        Assert.Contains("get_test@example.com", body);
        Assert.Contains("010-1010-2020", body);
    }

    [Fact]
    public async Task PostMultipleThenGetPaged_ReturnsCorrectPaging()
    {
        using var client = _factory.CreateClient();

        var payload = """
                      [
                        {"name":"가나다","email":"a@example.com","tel":"010-0001-0001","joined":"2020-01-01"},
                        {"name":"라마바","email":"b@example.com","tel":"010-0002-0002","joined":"2020-02-01"},
                        {"name":"사아자","email":"c@example.com","tel":"010-0003-0003","joined":"2020-03-01"},
                        {"name":"차카타","email":"d@example.com","tel":"010-0004-0004","joined":"2020-04-01"},
                        {"name":"파하거","email":"e@example.com","tel":"010-0005-0005","joined":"2020-05-01"}
                      ]
                      """;

        using var content = new StringContent(payload);
        content.Headers.ContentType = new("application/json");

        await client.PostAsync("/api/employee", content);

        var page1 = await client.GetFromJsonAsync<PagedResponse>("/api/employee?page=1&pageSize=2");
        Assert.NotNull(page1);
        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(1, page1.Page);
        Assert.Equal(2, page1.PageSize);

        var page3 = await client.GetFromJsonAsync<PagedResponse>("/api/employee?page=3&pageSize=2");
        Assert.NotNull(page3);
        Assert.Single(page3.Items);
    }

    // ──────────────── POST 실패 케이스 ────────────────

    [Fact]
    public async Task PostInvalidJson_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        using var content = new StringContent("{not-json}");
        content.Headers.ContentType = new("application/json");

        var response = await client.PostAsync("/api/employee", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("유효하지 않은 JSON 형식입니다", body);
    }

    [Fact]
    public async Task PostDuplicateEmail_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        var first = """{"name":"홍길동","email":"dup@example.com","tel":"010-1111-1111","joined":"2020-01-01"}""";
        using var c1 = new StringContent(first);
        c1.Headers.ContentType = new("application/json");
        await client.PostAsync("/api/employee", c1);

        var second = """{"name":"이몽룡","email":"dup@example.com","tel":"010-2222-2222","joined":"2021-01-01"}""";
        using var c2 = new StringContent(second);
        c2.Headers.ContentType = new("application/json");

        var response = await client.PostAsync("/api/employee", c2);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("중복", body);
    }

    [Fact]
    public async Task PostInvalidEmailFormat_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        var payload = """{"name":"테스트","email":"not-email","tel":"010-1234-5678","joined":"2020-01-01"}""";
        using var content = new StringContent(payload);
        content.Headers.ContentType = new("application/json");

        var response = await client.PostAsync("/api/employee", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("유효하지 않은 이메일 형식입니다", body);
    }

    [Fact]
    public async Task PostInvalidPhoneFormat_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        var payload = """{"name":"테스트","email":"test@example.com","tel":"12345","joined":"2020-01-01"}""";
        using var content = new StringContent(payload);
        content.Headers.ContentType = new("application/json");

        var response = await client.PostAsync("/api/employee", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("유효하지 않은 전화번호 형식입니다", body);
    }

    [Fact]
    public async Task PostEmptyCsvFile_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(""), "file", "empty.csv");

        var response = await client.PostAsync("/api/employee", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostUnsupportedFileType_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("some data"), "file", "data.xml");

        var response = await client.PostAsync("/api/employee", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(".csv 또는 .json 파일만 지원됩니다", body);
    }

    // ──────────────── GET 실패 케이스 ────────────────

    [Fact]
    public async Task GetPaged_WithPageZero_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/employee?page=0&pageSize=10");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("page와 pageSize는 0보다 커야 합니다", body);
    }

    [Fact]
    public async Task GetPaged_WithNegativePageSize_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/employee?page=1&pageSize=-1");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetByName_WhenMissing_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/employee/없는사용자");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByName_WhenNameHasSpecialChars_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/employee/특수!문자@테스트");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class PagedResponse
{
    public List<EmployeeResponse> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public class EmployeeResponse
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Tel { get; set; } = string.Empty;
    public string Joined { get; set; } = string.Empty;
}
