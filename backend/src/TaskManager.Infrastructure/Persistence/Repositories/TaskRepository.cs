using Microsoft.EntityFrameworkCore;

using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Repositories;

public sealed class TaskRepository(TaskManagerDbContext dbContext) : ITaskRepository
{
    public async Task<IReadOnlyList<TaskItem>> GetAllForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tasks = await dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.UserId == userId)
            .ToListAsync(cancellationToken);

        return tasks
            .OrderBy(task => task.Status)
            .ThenBy(task => task.DueDate)
            .ThenByDescending(task => task.CreatedAt)
            .ToArray();
    }

    public Task<TaskItem?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Tasks.SingleOrDefaultAsync(
            task => task.Id == id && task.UserId == userId,
            cancellationToken);
    }

    public async Task AddAsync(
        TaskItem task,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Tasks.AddAsync(task, cancellationToken);
    }

    public void Remove(TaskItem task)
    {
        dbContext.Tasks.Remove(task);
    }
}