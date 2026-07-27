using TaskManager.Domain.Enums;

namespace TaskManager.Application.Contracts.Tasks;

public sealed record CreateTaskRequest(
    string? Title,
    string? Description,
    TaskItemStatus? Status,
    DateOnly? DueDate);