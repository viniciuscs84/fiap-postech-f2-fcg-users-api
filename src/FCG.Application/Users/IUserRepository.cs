using FCG.Domain.Users;

namespace FCG.Application.Users;

/// <summary>Persistence boundary for user accounts.</summary>
public interface IUserRepository
{
    /// <summary>Finds a user by its normalized e-mail.</summary>
    Task<UserAccount?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Finds a user by its identifier.</summary>
    Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Lists all registered users.</summary>
    Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Adds a user to the current persistence unit of work.</summary>
    Task AddAsync(UserAccount user, CancellationToken cancellationToken);

    /// <summary>Marks a user for removal from the current persistence unit of work.</summary>
    void Remove(UserAccount user);

    /// <summary>Persists pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
