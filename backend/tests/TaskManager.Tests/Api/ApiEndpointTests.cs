using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using FluentAssertions;

using TaskManager.Application.Contracts.Auth;
using TaskManager.Application.Contracts.Tasks;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Tests.Api;

public sealed class ApiEndpointTests(TaskManagerApiFactory factory)
    : IClassFixture<TaskManagerApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    [Fact]
    public async Task HealthEndpoint_IsPublic()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/public/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutJwt_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithValidRequest_CreatesUser()
    {
        using var client = factory.CreateClient();
        var email = UniqueEmail();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("New User", email, "Password123!"));
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        using var client = factory.CreateClient();
        var request = new RegisterRequest("Duplicate User", UniqueEmail(), "Password123!");

        (await client.PostAsJsonAsync("/api/auth/register", request))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicateResponse = await client.PostAsJsonAsync("/api/auth/register", request);

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwt()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(DatabaseSeeder.DemoEmail, DatabaseSeeder.DemoPassword));
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(DatabaseSeeder.DemoEmail, "incorrect-password"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTask_WithValidRequest_ReturnsCreated()
    {
        using var client = factory.CreateClient();
        await AuthenticateAsDemoAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/tasks",
            ValidTaskRequest("Created through API"));
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        var listResponse = await client.GetAsync("/api/tasks");
        var tasks = await listResponse.Content
            .ReadFromJsonAsync<IReadOnlyList<TaskResponse>>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        task.Should().NotBeNull();
        task!.Title.Should().Be("Created through API");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        tasks.Should().NotBeNull();
        tasks!.Should().Contain(item => item.Id == task.Id);
    }

    [Fact]
    public async Task CreateTask_WithEmptyTitle_ReturnsBadRequest()
    {
        using var client = factory.CreateClient();
        await AuthenticateAsDemoAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/tasks",
            ValidTaskRequest(" "));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTask_WithTitleLongerThanOneHundredCharacters_ReturnsBadRequest()
    {
        using var client = factory.CreateClient();
        await AuthenticateAsDemoAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/tasks",
            ValidTaskRequest(new string('x', 101)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TaskOwnedByAnotherUser_CannotBeReadUpdatedOrDeleted()
    {
        using var ownerClient = factory.CreateClient();
        using var otherClient = factory.CreateClient();
        var ownerCredentials = await RegisterAndLoginAsync(ownerClient);
        await RegisterAndLoginAsync(otherClient);

        ownerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerCredentials.Token);

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/tasks",
            ValidTaskRequest("Private task"));
        var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);

        var getResponse = await otherClient.GetAsync($"/api/tasks/{task!.Id}");
        var updateResponse = await otherClient.PutAsJsonAsync(
            $"/api/tasks/{task.Id}",
            new UpdateTaskRequest(
                "Unauthorized update",
                null,
                TaskItemStatus.Completed,
                task.DueDate));
        var deleteResponse = await otherClient.DeleteAsync($"/api/tasks/{task.Id}");
        var ownerResponse = await ownerClient.GetAsync($"/api/tasks/{task.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ownerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateAndDelete_MissingTask_ReturnNotFound()
    {
        using var client = factory.CreateClient();
        await AuthenticateAsDemoAsync(client);
        var missingId = Guid.NewGuid();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/tasks/{missingId}",
            new UpdateTaskRequest(
                "Missing",
                null,
                TaskItemStatus.Pending,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))));
        var deleteResponse = await client.DeleteAsync($"/api/tasks/{missingId}");

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AuthenticatedUser_CanCompleteCrudLifecycle()
    {
        using var client = factory.CreateClient();
        await AuthenticateAsDemoAsync(client);

        var createResponse = await client.PostAsJsonAsync(
            "/api/tasks",
            ValidTaskRequest("CRUD lifecycle"));
        var created = await createResponse.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);

        var getResponse = await client.GetAsync($"/api/tasks/{created!.Id}");
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/tasks/{created.Id}",
            new UpdateTaskRequest(
                "CRUD lifecycle updated",
                "Completed in an integration test.",
                TaskItemStatus.Completed,
                created.DueDate));
        var deleteResponse = await client.DeleteAsync($"/api/tasks/{created.Id}");
        var afterDeleteResponse = await client.GetAsync($"/api/tasks/{created.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static CreateTaskRequest ValidTaskRequest(string title)
    {
        return new CreateTaskRequest(
            title,
            "API integration test",
            TaskItemStatus.Pending,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));
    }

    private static async Task AuthenticateAsDemoAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(DatabaseSeeder.DemoEmail, DatabaseSeeder.DemoPassword));
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        response.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.Token);
    }

    private static async Task<AuthResponse> RegisterAndLoginAsync(HttpClient client)
    {
        var email = UniqueEmail();
        var password = "Password123!";
        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Ownership Test", email, password));
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password));
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        loginResponse.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.Token);
        return auth;
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";
}