using FCG.Domain.Users;

namespace FCG.Application.Users;

/// <summary>Represents the result of user registration.</summary>
public abstract record RegistrationOutcome
{
    private RegistrationOutcome()
    {
    }

    /// <summary>Indicates successful registration.</summary>
    public sealed record Success(RegisteredUserResponse User) : RegistrationOutcome;

    /// <summary>Indicates validation failure.</summary>
    public sealed record ValidationFailure(IReadOnlyDictionary<string, string[]> Errors) : RegistrationOutcome;

    /// <summary>Indicates that the e-mail is already registered.</summary>
    public sealed record Conflict : RegistrationOutcome;
}

/// <summary>Represents a user returned after registration.</summary>
public sealed record RegisteredUserResponse(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    DateTime CreatedAtUtc);
