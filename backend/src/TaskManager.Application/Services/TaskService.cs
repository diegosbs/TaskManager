using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Services;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Contracts.Tasks;
using TaskManager.Application.Exceptions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Services;

public sealed class TaskService(
    ITaskRepository tasks,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IClock clock) : ITaskService
{
    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var userTasks = await tasks.GetAllForUserAsync(
            userContext.UserId,
            cancellationToken);

        return userTasks.Select(Map).ToArray();
    }

    public async Task<TaskResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Map(await GetOwnedTaskAsync(id, cancellationToken));
    }

    public async Task<TaskResponse> CreateAsync(
        CreateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var values = Validate(request.Title, request.Description, request.Status, request.DueDate);
        var task = new TaskItem(
            Guid.NewGuid(),
            values.Title,
            values.Description,
            values.Status,
            values.DueDate,
            userContext.UserId,
            clock.UtcNow);

        await tasks.AddAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(task);
    }

    public async Task<TaskResponse> UpdateAsync(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var values = Validate(request.Title, request.Description, request.Status, request.DueDate);
        var task = await GetOwnedTaskAsync(id, cancellationToken);

        task.Update(
            values.Title,
            values.Description,
            values.Status,
            values.DueDate,
            clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(task);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var task = await GetOwnedTaskAsync(id, cancellationToken);
        tasks.Remove(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<TaskItem> GetOwnedTaskAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdForUserAsync(
            id,
            userContext.UserId,
            cancellationToken);

        return task ?? throw new NotFoundException("Task was not found.");
    }

    private static ValidatedTaskValues Validate(
        string? title,
        string? description,
        TaskItemStatus? status,
        DateOnly? dueDate)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(title))
        {
            errors["title"] = ["Title is required."];
        }
        else if (title.Trim().Length > TaskItem.TitleMaxLength)
        {
            errors["title"] =
                [$"Title must not exceed {TaskItem.TitleMaxLength} characters."];
        }

        if (description?.Trim().Length > TaskItem.DescriptionMaxLength)
        {
            errors["description"] =
                [$"Description must not exceed {TaskItem.DescriptionMaxLength} characters."];
        }

        if (status is null || !Enum.IsDefined(status.Value))
        {
            errors["status"] = ["Status must be Pending, InProgress, or Completed."];
        }

        if (dueDate is null || dueDate == default(DateOnly))
        {
            errors["dueDate"] = ["Due date is required and must be a valid date."];
        }

        if (errors.Count > 0)
        {
            throw new ApplicationValidationException(errors);
        }

        return new ValidatedTaskValues(
            title!.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            status!.Value,
            dueDate!.Value);
    }

    private static TaskResponse Map(TaskItem task) =>
        new(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.DueDate,
            task.UserId,
            task.CreatedAt,
            task.UpdatedAt);

    private sealed record ValidatedTaskValues(
        string Title,
        string? Description,
        TaskItemStatus Status,
        DateOnly DueDate);
}