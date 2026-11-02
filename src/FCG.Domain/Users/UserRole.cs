namespace FCG.Domain.Users;

/// <summary>Defines the roles supported by the application.</summary>
public enum UserRole
{
    /// <summary>Standard customer role.</summary>
    User = 0,
    /// <summary>Administrator role with user, catalog and promotion management access.</summary>
    Administrator = 1
}
