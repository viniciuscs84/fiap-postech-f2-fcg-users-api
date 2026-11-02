namespace FCG.Application.Users;

/// <summary>Abstracts password hashing and verification.</summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plain-text password.</summary>
    /// <param name="password">Plain-text password.</param>
    /// <returns>The password hash.</returns>
    string HashPassword(string password);

    /// <summary>Checks a plain-text password against a stored hash.</summary>
    /// <param name="passwordHash">Stored password hash.</param>
    /// <param name="providedPassword">Password supplied by the user.</param>
    /// <returns><see langword="true"/> when the password matches.</returns>
    bool VerifyPassword(string passwordHash, string providedPassword);
}
