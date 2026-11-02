using FCG.Domain.Users;

namespace FCG.Application.Users;

/// <summary>Represents a user account exposed through administrative operations.</summary>
public sealed record AdminUserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAtUtc);

/// <summary>Command used by administrators to change a user's role.</summary>
public sealed record UpdateUserRoleCommand(string Role);

/// <summary>Possible outcomes for a user administration mutation.</summary>
public enum UserAdministrationResultKind
{
    /// <summary>Operation completed successfully.</summary>
    Success = 0,
    /// <summary>Target user was not found.</summary>
    NotFound = 1,
    /// <summary>Operation conflicts with an account rule.</summary>
    Conflict = 2
}

/// <summary>Result of an administrative user mutation.</summary>
public sealed record UserAdministrationResult(
    UserAdministrationResultKind Kind,
    AdminUserResponse? User = null,
    string? Detail = null);

/// <summary>Administrative use cases for user accounts.</summary>
public interface IUserAdministrationService
{
    /// <summary>Lists all registered users.</summary>
    Task<IReadOnlyList<AdminUserResponse>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Gets one registered user by identifier.</summary>
    Task<AdminUserResponse?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Changes the role of a registered user.</summary>
    Task<UserAdministrationResult> ChangeRoleAsync(
        Guid userId,
        Guid actingAdministratorId,
        UserRole role,
        CancellationToken cancellationToken);

    /// <summary>Deletes a registered user.</summary>
    Task<UserAdministrationResult> DeleteAsync(
        Guid userId,
        Guid actingAdministratorId,
        CancellationToken cancellationToken);
}

/// <summary>Coordinates administrative operations over user accounts.</summary>
public sealed class UserAdministrationService(IUserRepository repository) : IUserAdministrationService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminUserResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var users = await repository.ListAsync(cancellationToken);
        return users.Select(ToResponse).ToArray();
    }

    /// <inheritdoc />
    public async Task<AdminUserResponse?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await repository.FindByIdAsync(userId, cancellationToken);
        return user is null ? null : ToResponse(user);
    }

    /// <inheritdoc />
    public async Task<UserAdministrationResult> ChangeRoleAsync(
        Guid userId,
        Guid actingAdministratorId,
        UserRole role,
        CancellationToken cancellationToken)
    {
        if (userId == actingAdministratorId)
        {
            return new UserAdministrationResult(
                UserAdministrationResultKind.Conflict,
                Detail: "O administrador não pode alterar o próprio papel.");
        }

        var user = await repository.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return new UserAdministrationResult(UserAdministrationResultKind.NotFound);
        }

        user.ChangeRole(role);
        await repository.SaveChangesAsync(cancellationToken);

        return new UserAdministrationResult(UserAdministrationResultKind.Success, ToResponse(user));
    }

    /// <inheritdoc />
    public async Task<UserAdministrationResult> DeleteAsync(
        Guid userId,
        Guid actingAdministratorId,
        CancellationToken cancellationToken)
    {
        if (userId == actingAdministratorId)
        {
            return new UserAdministrationResult(
                UserAdministrationResultKind.Conflict,
                Detail: "O administrador não pode excluir a própria conta.");
        }

        var user = await repository.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return new UserAdministrationResult(UserAdministrationResultKind.NotFound);
        }

        repository.Remove(user);
        await repository.SaveChangesAsync(cancellationToken);

        return new UserAdministrationResult(UserAdministrationResultKind.Success);
    }

    private static AdminUserResponse ToResponse(UserAccount user) =>
        new(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAtUtc);
}
