using FluentAssertions;

using NSubstitute;

using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Contracts.Auth;
using TaskManager.Application.Exceptions;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;

namespace TaskManager.Tests.Application;

public sealed class AuthServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RegisterAsync_WithValidRequest_NormalizesAndPersistsUser()
    {
        var dependencies = CreateDependencies();
        User? addedUser = null;
        dependencies.Users
            .EmailExistsAsync("user@example.com", Arg.Any<CancellationToken>())
            .Returns(false);
        dependencies.PasswordHasher.Hash("Password123!").Returns("password-hash");
        dependencies.Users
            .AddAsync(
                Arg.Do<User>(user => addedUser = user),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await dependencies.Service.RegisterAsync(
            new RegisterRequest("  Test User  ", "  USER@Example.com  ", "Password123!"));

        result.Name.Should().Be("Test User");
        result.Email.Should().Be("user@example.com");
        result.CreatedAt.Should().Be(Now);
        addedUser.Should().NotBeNull();
        addedUser!.PasswordHash.Should().Be("password-hash");
        await dependencies.UnitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsConflictWithoutPersisting()
    {
        var dependencies = CreateDependencies();
        dependencies.Users
            .EmailExistsAsync("existing@example.com", Arg.Any<CancellationToken>())
            .Returns(true);

        Func<Task> act = () => dependencies.Service.RegisterAsync(
            new RegisterRequest("Existing User", "EXISTING@example.com", "Password123!"));

        await act.Should().ThrowAsync<ConflictException>();
        await dependencies.Users.DidNotReceive()
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await dependencies.UnitOfWork.DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidFields_ReturnsAllValidationErrors()
    {
        var dependencies = CreateDependencies();

        Func<Task> act = () => dependencies.Service.RegisterAsync(
            new RegisterRequest(" ", "invalid-email", "short"));

        var exception = await act.Should().ThrowAsync<ApplicationValidationException>();
        exception.Which.Errors.Keys.Should().BeEquivalentTo("name", "email", "password");
        await dependencies.Users.DidNotReceive()
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndUser()
    {
        var dependencies = CreateDependencies();
        var user = CreateUser();
        dependencies.Users
            .GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>())
            .Returns(user);
        dependencies.PasswordHasher.Verify("Password123!", user.PasswordHash).Returns(true);
        dependencies.TokenService.CreateToken(user).Returns("jwt-token");

        var result = await dependencies.Service.LoginAsync(
            new LoginRequest(" USER@example.com ", "Password123!"));

        result.Token.Should().Be("jwt-token");
        result.User.Id.Should().Be(user.Id);
        result.User.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorized()
    {
        var dependencies = CreateDependencies();
        var user = CreateUser();
        dependencies.Users
            .GetByEmailAsync(user.Email, Arg.Any<CancellationToken>())
            .Returns(user);
        dependencies.PasswordHasher.Verify("incorrect", user.PasswordHash).Returns(false);

        Func<Task> act = () => dependencies.Service.LoginAsync(
            new LoginRequest(user.Email, "incorrect"));

        await act.Should().ThrowAsync<UnauthorizedException>();
        dependencies.TokenService.DidNotReceive().CreateToken(Arg.Any<User>());
    }

    private static AuthDependencies CreateDependencies()
    {
        var users = Substitute.For<IUserRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var tokenService = Substitute.For<ITokenService>();
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);

        return new AuthDependencies(
            new AuthService(users, unitOfWork, passwordHasher, tokenService, clock),
            users,
            unitOfWork,
            passwordHasher,
            tokenService);
    }

    private static User CreateUser()
    {
        return new User(
            Guid.NewGuid(),
            "Test User",
            "user@example.com",
            "password-hash",
            Now);
    }

    private sealed record AuthDependencies(
        AuthService Service,
        IUserRepository Users,
        IUnitOfWork UnitOfWork,
        IPasswordHasher PasswordHasher,
        ITokenService TokenService);
}