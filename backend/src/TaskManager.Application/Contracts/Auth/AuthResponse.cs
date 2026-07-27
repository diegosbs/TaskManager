namespace TaskManager.Application.Contracts.Auth;

public sealed record AuthResponse(string Token, UserResponse User);