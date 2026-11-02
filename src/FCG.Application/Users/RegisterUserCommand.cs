namespace FCG.Application.Users;

/// <summary>Contains the data required to register a user.</summary>
public sealed record RegisterUserCommand(string Name, string Email, string Password);
