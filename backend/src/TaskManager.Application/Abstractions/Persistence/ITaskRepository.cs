using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions.Persistence;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> GetAllForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<TaskItem?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);

    void Remove(TaskItem task);
}