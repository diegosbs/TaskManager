using TaskManager.Domain.Enums;

namespace TaskManager.Application.Contracts.Tasks;

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateOnly DueDate,
    Guid UserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);