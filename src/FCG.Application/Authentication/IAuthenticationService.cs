namespace FCG.Application.Authentication;

/// <summary>Coordinates user authentication.</summary>
public interface IAuthenticationService
{
    /// <summary>Authenticates a user and returns an outcome containing a token on success.</summary>
    /// <param name="command">Credentials to validate.</param>
    /// <param name="cancellationToken">Token that cancels the operation.</param>
    /// <returns>The authentication outcome.</returns>
    Task<LoginOutcome> LoginAsync(LoginCommand command, CancellationToken cancellationToken);
}
