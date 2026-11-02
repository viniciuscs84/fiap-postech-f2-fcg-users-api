using FCG.Application.Users;

namespace FCG.Application.Authentication;

/// <summary>Implements credential validation and token issuance.</summary>
public sealed class AuthenticationService(IUserRepository repository, IPasswordHasher passwordHasher, ITokenService tokenService) : IAuthenticationService
{
    /// <inheritdoc />
    public async Task<LoginOutcome> LoginAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        if (!RegistrationRules.IsValidEmail(command.Email) || string.IsNullOrWhiteSpace(command.Password))
        {
            return new LoginOutcome.Failure();
        }

        var normalizedEmail = RegistrationRules.NormalizeEmail(command.Email);
        var user = await repository.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !passwordHasher.VerifyPassword(user.PasswordHash, command.Password))
        {
            return new LoginOutcome.Failure();
        }

        var token = tokenService.CreateToken(user.Id, user.Email, user.Role);
        return new LoginOutcome.Success(token);
    }
}
