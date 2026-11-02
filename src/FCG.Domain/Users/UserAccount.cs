namespace FCG.Domain.Users;

/// <summary>Represents an authenticated FCG user account.</summary>
public sealed class UserAccount
{
    private UserAccount()
    {
    }

    /// <summary>Gets the unique account identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the user's display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the user's original e-mail address.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Gets the canonical e-mail value used for lookups.</summary>
    public string NormalizedEmail { get; private set; } = string.Empty;

    /// <summary>Gets the stored password hash.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Gets the account role.</summary>
    public UserRole Role { get; private set; }

    /// <summary>Gets the account creation timestamp in UTC.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Creates a new user account with normalized identity data.</summary>
    /// <param name="name">Display name.</param>
    /// <param name="email">E-mail address.</param>
    /// <param name="passwordHash">Already hashed password.</param>
    /// <param name="role">Role assigned to the account.</param>
    /// <param name="createdAtUtc">Creation timestamp.</param>
    /// <returns>A new user account.</returns>
    public static UserAccount Register(string name, string email, string passwordHash, UserRole role, DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var trimmedName = name.Trim();
        var trimmedEmail = email.Trim();

        return new UserAccount
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            Email = trimmedEmail,
            NormalizedEmail = trimmedEmail.ToUpperInvariant(),
            PasswordHash = passwordHash,
            Role = role,
            CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc)
        };
    }

    /// <summary>Changes the authorization role assigned to the account.</summary>
    /// <param name="role">New role to assign.</param>
    public void ChangeRole(UserRole role)
    {
        Role = role;
    }
}
