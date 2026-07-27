using FluentAssertions;

using NSubstitute;

using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Contracts.Tasks;
using TaskManager.Application.Exceptions;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Tests.Application;

public sealed class TaskServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Now =
        new(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly DueDate = new(2026, 8, 1);

    [Fact]
    public async Task GetAllAsync_RequestsOnlyAuthenticatedUsersTasks()
    {
        var dependencies = CreateDependencies();
        var task = CreateTask("Owned task");
        dependencies.Tasks
            .GetAllForUserAsync(UserId, Arg.Any<CancellationToken>())
            .Returns([task]);

        var result = await dependencies.Service.GetAllAsync();

        result.Should().ContainSingle();
        result[0].Id.Should().Be(task.Id);
        result[0].UserId.Should().Be(UserId);
        await dependencies.Tasks.Received(1)
            .GetAllForUserAsync(UserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenOwnedTaskDoesNotExist_ThrowsNotFound()
    {
        var dependencies = CreateDependencies();
        var taskId = Guid.NewGuid();
        dependencies.Tasks
            .GetByIdForUserAsync(taskId, UserId, Arg.Any<CancellationToken>())
            .Returns((TaskItem?)null);

        Func<Task> act = () => dependencies.Service.GetByIdAsync(taskId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_NormalizesAndPersistsOwnedTask()
    {
        var dependencies = CreateDependencies();
        TaskItem? addedTask = null;
        dependencies.Tasks
            .AddAsync(
                Arg.Do<TaskItem>(task => addedTask = task),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await dependencies.Service.CreateAsync(
            new CreateTaskRequest(
                "  New task  ",
                "  Details  ",
                TaskItemStatus.Pending,
                DueDate));

        result.Title.Should().Be("New task");
        result.Description.Should().Be("Details");
        result.UserId.Should().Be(UserId);
        result.CreatedAt.Should().Be(Now);
        addedTask.Should().NotBeNull();
        addedTask!.Id.Should().Be(result.Id);
        await dependencies.UnitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidFields_ReturnsAllValidationErrors()
    {
        var dependencies = CreateDependencies();

        Func<Task> act = () => dependencies.Service.CreateAsync(
            new CreateTaskRequest(
                " ",
                new string('x', TaskItem.DescriptionMaxLength + 1),
                (TaskItemStatus)999,
                null));

        var exception = await act.Should().ThrowAsync<ApplicationValidationException>();
        exception.Which.Errors.Keys.Should()
            .BeEquivalentTo("title", "description", "status", "dueDate");
        await dependencies.Tasks.DidNotReceive()
            .AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
        await dependencies.UnitOfWork.DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithOwnedTask_UpdatesAndPersistsEntity()
    {
        var dependencies = CreateDependencies();
        var task = CreateTask("Original title");
        dependencies.Tasks
            .GetByIdForUserAsync(task.Id, UserId, Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await dependencies.Service.UpdateAsync(
            task.Id,
            new UpdateTaskRequest(
                "Completed task",
                "Done",
                TaskItemStatus.Completed,
                DueDate.AddDays(1)));

        result.Title.Should().Be("Completed task");
        result.Status.Should().Be(TaskItemStatus.Completed);
        result.UpdatedAt.Should().Be(Now);
        task.Title.Should().Be(result.Title);
        await dependencies.UnitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WithOwnedTask_RemovesAndPersistsEntity()
    {
        var dependencies = CreateDependencies();
        var task = CreateTask("Delete me");
        dependencies.Tasks
            .GetByIdForUserAsync(task.Id, UserId, Arg.Any<CancellationToken>())
            .Returns(task);

        await dependencies.Service.DeleteAsync(task.Id);

        dependencies.Tasks.Received(1).Remove(task);
        await dependencies.UnitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static TaskDependencies CreateDependencies()
    {
        var tasks = Substitute.For<ITaskRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var userContext = Substitute.For<IUserContext>();
        var clock = Substitute.For<IClock>();
        userContext.UserId.Returns(UserId);
        clock.UtcNow.Returns(Now);

        return new TaskDependencies(
            new TaskService(tasks, unitOfWork, userContext, clock),
            tasks,
            unitOfWork);
    }

    private static TaskItem CreateTask(string title)
    {
        return new TaskItem(
            Guid.NewGuid(),
            title,
            null,
            TaskItemStatus.Pending,
            DueDate,
            UserId,
            Now.AddHours(-1));
    }

    private sealed record TaskDependencies(
        TaskService Service,
        ITaskRepository Tasks,
        IUnitOfWork UnitOfWork);
}