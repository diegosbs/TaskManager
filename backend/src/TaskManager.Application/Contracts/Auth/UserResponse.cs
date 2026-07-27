namespace TaskManager.Application.Contracts.Auth;

public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    DateTimeOffset CreatedAt);