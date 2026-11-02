using FCG.Application.Users;
using FCG.Domain.Users;

namespace FCG.Tests.Users;

public sealed class UserRegistrationServiceTests
{
    [Fact]
    public async Task RegisterAsync_returns_conflict_when_email_already_exists()
    {
        var repository = new InMemoryUserRepository(
            [UserAccount.Register("Existing", "existing@example.com", "hash", UserRole.User, DateTime.UtcNow)]);
        var service = new UserRegistrationService(repository, new FakePasswordHasher());

        var result = await service.RegisterAsync(new RegisterUserCommand("Alice", "existing@example.com", "Password1!"), CancellationToken.None);

        Assert.IsType<RegistrationOutcome.Conflict>(result);
    }

    [Fact]
    public async Task RegisterAsync_persists_hashed_password_and_default_role()
    {
        var repository = new InMemoryUserRepository();
        var service = new UserRegistrationService(repository, new FakePasswordHasher());

        var result = await service.RegisterAsync(new RegisterUserCommand("  Alice  ", "alice@example.com", "Password1!"), CancellationToken.None);

        var success = Assert.IsType<RegistrationOutcome.Success>(result);
        Assert.Equal("Alice", success.User.Name);
        Assert.Equal("alice@example.com", success.User.Email);
        Assert.Equal(UserRole.User, success.User.Role);
        Assert.Equal("hashed:Password1!", repository.Users.Single().PasswordHash);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hashed:{password}";

        public bool VerifyPassword(string passwordHash, string providedPassword) => passwordHash == $"hashed:{providedPassword}";
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly List<UserAccount> users;

        public InMemoryUserRepository()
        {
            users = [];
        }

        public InMemoryUserRepository(IEnumerable<UserAccount> seedUsers)
        {
            users = seedUsers.ToList();
        }

        public IReadOnlyList<UserAccount> Users => users;

        public Task<UserAccount?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            return Task.FromResult(users.SingleOrDefault(user => user.NormalizedEmail == normalizedEmail));
        }

        public Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(users.SingleOrDefault(user => user.Id == userId));
        }

        public Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<UserAccount>>(users);
        }

        public Task AddAsync(UserAccount user, CancellationToken cancellationToken)
        {
            users.Add(user);
            return Task.CompletedTask;
        }

        public void Remove(UserAccount user)
        {
            users.Remove(user);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }
}
