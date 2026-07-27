using System.Net.Mail;

using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Abstractions.Services;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Contracts.Auth;
using TaskManager.Application.Exceptions;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Services;

public sealed class AuthService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IClock clock) : IAuthService
{
    public async Task<UserResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRegistration(request);

        var email = NormalizeEmail(request.Email!);
        if (await users.EmailExistsAsync(email, cancellationToken))
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var user = new User(
            Guid.NewGuid(),
            request.Name!,
            email,
            passwordHasher.Hash(request.Password!),
            clock.UtcNow);

        await users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(user);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var user = await users.GetByEmailAsync(
            NormalizeEmail(request.Email),
            cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return new AuthResponse(tokenService.CreateToken(user), Map(user));
    }

    private static void ValidateRegistration(RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = ["Name is required."];
        }
        else if (request.Name.Trim().Length > 100)
        {
            errors["name"] = ["Name must not exceed 100 characters."];
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["email"] = ["Email is required."];
        }
        else if (!IsValidEmail(request.Email))
        {
            errors["email"] = ["Email must be a valid email address."];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = ["Password is required."];
        }
        else if (request.Password.Length is < 8 or > 128)
        {
            errors["password"] = ["Password must contain between 8 and 128 characters."];
        }

        if (errors.Count > 0)
        {
            throw new ApplicationValidationException(errors);
        }
    }

    private static bool IsValidEmail(string value)
    {
        try
        {
            var address = new MailAddress(value.Trim());
            return string.Equals(
                address.Address,
                value.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    private static UserResponse Map(User user) =>
        new(user.Id, user.Name, user.Email, user.CreatedAt);
}