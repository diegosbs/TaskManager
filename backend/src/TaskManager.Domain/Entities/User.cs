using TaskManager.Domain.Exceptions;

namespace TaskManager.Domain.Entities;

public sealed class User
{
    private User()
    {
    }

    public User(
        Guid id,
        string name,
        string email,
        string passwordHash,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("User id is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("User name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainValidationException("User email is required.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainValidationException("Password hash is required.");
        }

        Id = id;
        Name = name.Trim();
        Email = email.Trim();
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }
}