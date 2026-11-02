using FCG.Application.Users;

namespace FCG.Tests.Users;

public sealed class RegistrationRulesTests
{
    [Theory]
    [InlineData("Alice", "alice@example.com", "Password1!")]
    [InlineData("  Alice  ", "  alice@example.com  ", "Abcdef1!")]
    public void Validate_returns_no_errors_for_valid_input(string name, string email, string password)
    {
        var result = RegistrationRules.Validate(new RegisterUserCommand(name, email, password));

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_returns_email_error_for_invalid_email(string? email)
    {
        var result = RegistrationRules.Validate(new RegisterUserCommand("Alice", email ?? string.Empty, "Password1!"));

        Assert.Contains(result, pair => pair.Key == nameof(RegisterUserCommand.Email));
    }

    [Theory]
    [InlineData("short1!")]
    [InlineData("longpassword")]
    [InlineData("12345678")]
    [InlineData("Password")]
    public void Validate_returns_password_error_for_invalid_password(string password)
    {
        var result = RegistrationRules.Validate(new RegisterUserCommand("Alice", "alice@example.com", password));

        Assert.Contains(result, pair => pair.Key == nameof(RegisterUserCommand.Password));
    }
}
