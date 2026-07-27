using TaskManager.Domain.Enums;

namespace TaskManager.Application.Contracts.Tasks;

public sealed record UpdateTaskRequest(
    string? Title,
    string? Description,
    TaskItemStatus? Status,
    DateOnly? DueDate);