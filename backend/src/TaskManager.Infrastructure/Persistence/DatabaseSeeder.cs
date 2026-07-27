using Microsoft.EntityFrameworkCore;

using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Infrastructure.Persistence;

public sealed class DatabaseSeeder(
    TaskManagerDbContext dbContext,
    IPasswordHasher passwordHasher,
    IClock clock)
{
    public const string DemoEmail = "demo@taskmanager.local";
    public const string DemoPassword = "Demo123!";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(
            user => user.Email == DemoEmail,
            cancellationToken);

        var now = clock.UtcNow;
        if (user is null)
        {
            user = new User(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Demo User",
                DemoEmail,
                passwordHasher.Hash(DemoPassword),
                now);

            await dbContext.Users.AddAsync(user, cancellationToken);
        }

        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var tasks = new[]
        {
            new TaskItem(
                Guid.Parse("22222222-2222-2222-2222-222222222221"),
                "Review the Clean Architecture boundaries",
                "Inspect project references and dependency direction.",
                TaskItemStatus.InProgress,
                today.AddDays(3),
                user.Id,
                now),
            new TaskItem(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Run the automated test suite",
                "Execute dotnet test before the interview.",
                TaskItemStatus.Pending,
                today.AddDays(5),
                user.Id,
                now)
        };

        var taskIds = tasks.Select(task => task.Id).ToArray();
        var existingTaskIds = await dbContext.Tasks
            .Where(task => taskIds.Contains(task.Id))
            .Select(task => task.Id)
            .ToListAsync(cancellationToken);
        var missingTasks = tasks
            .Where(task => !existingTaskIds.Contains(task.Id))
            .ToArray();

        if (missingTasks.Length > 0)
        {
            await dbContext.Tasks.AddRangeAsync(missingTasks, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}