using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManagerCICD.Models;

namespace Tests.Integration;

// Testa a API por HTTP: rotas, códigos de estado e JSON (o que os testes unitários não cobrem)
public class TasksApiTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<TaskItem> CreateTaskAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new { title = "Tarefa de teste" }, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskItem>(Json, Ct))!;
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/tasks", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidData_Returns201WithLocation()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new { title = "Nova tarefa", priority = "High" }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var json = await response.Content.ReadAsStringAsync(Ct);
        Assert.Contains("\"priority\":\"High\"", json);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithEmptyTitle_Returns400(string title)
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new { title }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidPriority_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new { title = "Tarefa", priority = "Urgent" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/tasks/999999", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Complete_Twice_Returns409()
    {
        var task = await CreateTaskAsync();

        var first = await _client.PutAsync($"/api/tasks/{task.Id}/complete", null, Ct);
        var second = await _client.PutAsync($"/api/tasks/{task.Id}/complete", null, Ct);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Complete_UnknownId_Returns404()
    {
        var response = await _client.PutAsync("/api/tasks/999999/complete", null, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingTask_Returns204ThenGetReturns404()
    {
        var task = await CreateTaskAsync();

        var delete = await _client.DeleteAsync($"/api/tasks/{task.Id}", Ct);
        var get = await _client.GetAsync($"/api/tasks/{task.Id}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Version_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/version", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
