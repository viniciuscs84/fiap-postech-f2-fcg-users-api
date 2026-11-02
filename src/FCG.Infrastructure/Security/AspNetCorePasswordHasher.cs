using FCG.Application.Users;
using Microsoft.AspNetCore.Identity;

namespace FCG.Infrastructure.Security;

/// <summary>Provides password hashing through ASP.NET Core Identity.</summary>
public sealed class AspNetCorePasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> hasher = new();

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        return hasher.HashPassword(new object(), password);
    }

    /// <inheritdoc />
    public bool VerifyPassword(string passwordHash, string providedPassword)
    {
        return hasher.VerifyHashedPassword(new object(), passwordHash, providedPassword) is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
