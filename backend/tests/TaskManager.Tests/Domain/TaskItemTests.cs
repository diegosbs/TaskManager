using FluentAssertions;

using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Tests.Domain;

public sealed class TaskItemTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesTask()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var task = new TaskItem(
            Guid.NewGuid(),
            "Prepare interview",
            "Review the solution.",
            TaskItemStatus.Pending,
            new DateOnly(2026, 8, 1),
            userId,
            now);

        task.Title.Should().Be("Prepare interview");
        task.UserId.Should().Be(userId);
        task.CreatedAt.Should().Be(now);
        task.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Constructor_WithEmptyTitle_ThrowsValidationException()
    {
        var action = () => CreateTask(" ");

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("*title is required*");
    }

    [Fact]
    public void Constructor_WithTitleLongerThanOneHundredCharacters_ThrowsValidationException()
    {
        var action = () => CreateTask(new string('x', 101));

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("*100 characters*");
    }

    private static TaskItem CreateTask(string title)
    {
        return new TaskItem(
            Guid.NewGuid(),
            title,
            null,
            TaskItemStatus.Pending,
            new DateOnly(2026, 8, 1),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
    }
}