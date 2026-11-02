namespace FCG.Application.Users;

/// <summary>Coordinates user registration.</summary>
public interface IUserRegistrationService
{
    /// <summary>Validates and registers a new user.</summary>
    /// <param name="command">Registration data.</param>
    /// <param name="cancellationToken">Token that cancels the operation.</param>
    /// <returns>The registration outcome.</returns>
    Task<RegistrationOutcome> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken);
}
