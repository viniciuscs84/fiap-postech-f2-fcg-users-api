using FCG.Domain.Users;

namespace FCG.Application.Users;

/// <summary>Implements the user registration use case.</summary>
public sealed class UserRegistrationService(IUserRepository repository, IPasswordHasher passwordHasher) : IUserRegistrationService
{
    /// <inheritdoc />
    public async Task<RegistrationOutcome> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var validationErrors = RegistrationRules.Validate(command);
        if (validationErrors.Count > 0)
        {
            return new RegistrationOutcome.ValidationFailure(validationErrors);
        }

        var normalizedEmail = RegistrationRules.NormalizeEmail(command.Email);
        var existingUser = await repository.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (existingUser is not null)
        {
            return new RegistrationOutcome.Conflict();
        }

        var passwordHash = passwordHasher.HashPassword(command.Password);
        var user = UserAccount.Register(
            RegistrationRules.NormalizeName(command.Name),
            command.Email,
            passwordHash,
            UserRole.User,
            DateTime.UtcNow);

        await repository.AddAsync(user, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return new RegistrationOutcome.Success(new RegisteredUserResponse(
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.CreatedAtUtc));
    }
}
