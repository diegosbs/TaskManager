using FluentAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Persistence.Repositories;

namespace TaskManager.Tests.Infrastructure;

public sealed class RepositoryTests
{
    [Fact]
    public async Task TaskRepository_FiltersReadsByOwner()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TaskManagerDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new TaskManagerDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var firstUser = CreateUser("first@example.com");
        var secondUser = CreateUser("second@example.com");
        await dbContext.Users.AddRangeAsync(firstUser, secondUser);

        var firstTask = CreateTask(firstUser.Id, "First user's task");
        var secondTask = CreateTask(secondUser.Id, "Second user's task");
        await dbContext.Tasks.AddRangeAsync(firstTask, secondTask);
        await dbContext.SaveChangesAsync();

        var repository = new TaskRepository(dbContext);
        var firstUserTasks = await repository.GetAllForUserAsync(firstUser.Id);
        var hiddenTask = await repository.GetByIdForUserAsync(
            secondTask.Id,
            firstUser.Id);

        firstUserTasks.Should().ContainSingle()
            .Which.Id.Should().Be(firstTask.Id);
        hiddenTask.Should().BeNull();
    }

    [Fact]
    public async Task UserRepository_EmailLookupUsesCaseInsensitiveUniqueStorage()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TaskManagerDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new TaskManagerDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        await dbContext.Users.AddAsync(CreateUser("owner@example.com"));
        await dbContext.SaveChangesAsync();

        var repository = new UserRepository(dbContext);

        (await repository.EmailExistsAsync("OWNER@EXAMPLE.COM")).Should().BeTrue();
    }

    private static User CreateUser(string email)
    {
        return new User(
            Guid.NewGuid(),
            "Test User",
            email,
            "not-a-real-test-hash",
            DateTimeOffset.UtcNow);
    }

    private static TaskItem CreateTask(Guid userId, string title)
    {
        return new TaskItem(
            Guid.NewGuid(),
            title,
            null,
            TaskItemStatus.Pending,
            new DateOnly(2026, 8, 1),
            userId,
            DateTimeOffset.UtcNow);
    }
}