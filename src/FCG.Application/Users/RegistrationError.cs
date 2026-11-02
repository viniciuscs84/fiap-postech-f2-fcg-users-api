namespace FCG.Application.Users;

/// <summary>Describes a validation error associated with a field.</summary>
public sealed record RegistrationError(string Field, string Message);
