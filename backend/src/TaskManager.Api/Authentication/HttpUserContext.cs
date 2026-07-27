using System.Security.Claims;

using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Exceptions;

namespace TaskManager.Api.Authentication;

public sealed class HttpUserContext(IHttpContextAccessor httpContextAccessor)
    : IUserContext
{
    public Guid UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var userId)
                ? userId
                : throw new UnauthorizedException("A valid authenticated user is required.");
        }
    }
}