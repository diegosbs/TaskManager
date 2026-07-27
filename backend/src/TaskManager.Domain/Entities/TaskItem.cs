using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Domain.Entities;

public sealed class TaskItem
{
    public const int TitleMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    private TaskItem()
    {
    }

    public TaskItem(
        Guid id,
        string title,
        string? description,
        TaskItemStatus status,
        DateOnly dueDate,
        Guid userId,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Task id is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainValidationException("A task must belong to a user.");
        }

        Validate(title, description, status, dueDate);

        Id = id;
        UserId = userId;
        Title = title.Trim();
        Description = NormalizeDescription(description);
        Status = status;
        DueDate = dueDate;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public TaskItemStatus Status { get; private set; }

    public DateOnly DueDate { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public User? User { get; private set; }

    public void Update(
        string title,
        string? description,
        TaskItemStatus status,
        DateOnly dueDate,
        DateTimeOffset updatedAt)
    {
        Validate(title, description, status, dueDate);

        Title = title.Trim();
        Description = NormalizeDescription(description);
        Status = status;
        DueDate = dueDate;
        UpdatedAt = updatedAt;
    }

    private static void Validate(
        string title,
        string? description,
        TaskItemStatus status,
        DateOnly dueDate)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("Task title is required.");
        }

        if (title.Trim().Length > TitleMaxLength)
        {
            throw new DomainValidationException(
                $"Task title must not exceed {TitleMaxLength} characters.");
        }

        if (description?.Trim().Length > DescriptionMaxLength)
        {
            throw new DomainValidationException(
                $"Task description must not exceed {DescriptionMaxLength} characters.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new DomainValidationException("Task status is invalid.");
        }

        if (dueDate == default)
        {
            throw new DomainValidationException("Task due date is required.");
        }
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}