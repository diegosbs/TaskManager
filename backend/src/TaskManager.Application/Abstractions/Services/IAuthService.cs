using TaskManager.Application.Contracts.Auth;

namespace TaskManager.Application.Abstractions.Services;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}