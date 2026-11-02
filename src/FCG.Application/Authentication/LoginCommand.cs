namespace FCG.Application.Authentication;

/// <summary>Contains credentials used to authenticate a user.</summary>
public sealed record LoginCommand(string Email, string Password);
