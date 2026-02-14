using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Integration;

public sealed class EmployeeApiIntegrationTests
{
    [Fact]
    public async Task PostJsonBody_ThenGetByName_ReturnsInsertedEmployee()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var payload = """
                      {"name":"홍길동","email":"user10@example.com","tel":"010-1111-2222","joined":"2020-05-01"}
                      """;

        using var content = new StringContent(payload);
        content.Headers.ContentType = new("application/json");

        var post = await client.PostAsync("/api/employee", content);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var get = await client.GetAsync("/api/employee/홍길동");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var body = await get.Content.ReadAsStringAsync();
        Assert.Contains("홍길동", body);
        Assert.Contains("user10@example.com", body);
    }

    [Fact]
    public async Task PostMultipartCsv_ThenGetPaged_ReturnsInsertedCount()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        const string csv = "name,email,tel,joined\n김철수,user11@example.com,01012341234,2018.03.07";

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(csv), "file", "employees.csv");

        var post = await client.PostAsync("/api/employee", form);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var page = await client.GetFromJsonAsync<PagedResponse>("/api/employee?page=1&pageSize=10");
        Assert.NotNull(page);
        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task PostInvalidJson_ReturnsBadRequest()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var content = new StringContent("{not-json}");
        content.Headers.ContentType = new("application/json");

        var response = await client.PostAsync("/api/employee", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid json payload", body);
    }


    [Fact]
    public async Task PostFormFieldData_WithJsonPayload_ReturnsCreated()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        const string json = """
                            {"name":"성춘향","email":"user12@example.com","tel":"010-2222-3333","joined":"2021-11-30"}
                            """;

        using var form = new MultipartFormDataContent
        {
            { new StringContent(json), "data" }
        };

        var response = await client.PostAsync("/api/employee", form);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetPaged_WithInvalidPage_ReturnsBadRequest()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/employee?page=0&pageSize=10");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("page and pageSize must be greater than zero", body);
    }
    [Fact]
    public async Task GetByName_WhenMissing_ReturnsNotFound()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/employee/없는사용자");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record PagedResponse(List<EmployeeResponse> Items, int Page, int PageSize, int TotalCount);

    private sealed record EmployeeResponse(string Name, string Email, string Tel, string Joined);
}
