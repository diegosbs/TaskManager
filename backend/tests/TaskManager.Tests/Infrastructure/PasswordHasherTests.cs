using FluentAssertions;

using TaskManager.Infrastructure.Security;

namespace TaskManager.Tests.Infrastructure;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_ProducesSaltedHashAndVerifiesPassword()
    {
        var hasher = new Pbkdf2PasswordHasher();

        var firstHash = hasher.Hash("StrongPassword!");
        var secondHash = hasher.Hash("StrongPassword!");

        firstHash.Should().NotBe("StrongPassword!");
        secondHash.Should().NotBe(firstHash);
        hasher.Verify("StrongPassword!", firstHash).Should().BeTrue();
        hasher.Verify("wrong-password", firstHash).Should().BeFalse();
    }
}