using FluentAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using NSubstitute;

using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Tests.Infrastructure;

public sealed class DatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_WhenDemoUserAlreadyExists_AddsMissingTasksWithoutDuplicates()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TaskManagerDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var dbContext = new TaskManagerDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var existingUser = new User(
            Guid.NewGuid(),
            "Existing Demo",
            DatabaseSeeder.DemoEmail,
            "existing-password-hash",
            DateTimeOffset.UtcNow);
        await dbContext.Users.AddAsync(existingUser);
        await dbContext.SaveChangesAsync();

        var passwordHasher = Substitute.For<IPasswordHasher>();
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(new DateTimeOffset(2026, 7, 27, 12, 0, 0, TimeSpan.Zero));
        var seeder = new DatabaseSeeder(dbContext, passwordHasher, clock);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        (await dbContext.Users.CountAsync()).Should().Be(1);
        var tasks = await dbContext.Tasks.ToListAsync();
        tasks.Should().HaveCount(2);
        tasks.Should().OnlyContain(task => task.UserId == existingUser.Id);
        passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
    }
}