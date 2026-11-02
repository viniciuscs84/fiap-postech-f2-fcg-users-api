using FCG.Domain.Users;

namespace FCG.Application.Authentication;

/// <summary>Creates access tokens for authenticated users.</summary>
public interface ITokenService
{
    /// <summary>Creates a signed token containing the user's identity and role.</summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="email">User e-mail address.</param>
    /// <param name="role">User role.</param>
    /// <returns>A serialized access token.</returns>
    string CreateToken(Guid userId, string email, UserRole role);
}
