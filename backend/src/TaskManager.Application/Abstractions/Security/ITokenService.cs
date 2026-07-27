using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions.Security;

public interface ITokenService
{
    string CreateToken(User user);
}