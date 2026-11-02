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
        var publisher = new RecordingUserCreatedEventPublisher();
        var service = new UserRegistrationService(repository, new FakePasswordHasher(), publisher);

        var result = await service.RegisterAsync(new RegisterUserCommand("Alice", "existing@example.com", "Password1!"), CancellationToken.None);

        Assert.IsType<RegistrationOutcome.Conflict>(result);
        Assert.Empty(publisher.PublishedEvents);
    }

    [Fact]
    public async Task RegisterAsync_persists_hashed_password_and_default_role()
    {
        var repository = new InMemoryUserRepository();
        var publisher = new RecordingUserCreatedEventPublisher(() => Assert.True(repository.ChangesSaved));
        var service = new UserRegistrationService(repository, new FakePasswordHasher(), publisher);

        var result = await service.RegisterAsync(new RegisterUserCommand("  Alice  ", "alice@example.com", "Password1!"), CancellationToken.None);

        var success = Assert.IsType<RegistrationOutcome.Success>(result);
        Assert.Equal("Alice", success.User.Name);
        Assert.Equal("alice@example.com", success.User.Email);
        Assert.Equal(UserRole.User, success.User.Role);
        Assert.Equal("hashed:Password1!", repository.Users.Single().PasswordHash);
        Assert.True(success.EventPublished);
        var publishedEvent = Assert.Single(publisher.PublishedEvents);
        Assert.Equal(success.User.Id, publishedEvent.UserId);
        Assert.Equal(success.User.Email, publishedEvent.Email);
    }

    [Fact]
    public async Task RegisterAsync_does_not_publish_when_validation_fails()
    {
        var publisher = new RecordingUserCreatedEventPublisher();
        var service = new UserRegistrationService(new InMemoryUserRepository(), new FakePasswordHasher(), publisher);

        var result = await service.RegisterAsync(
            new RegisterUserCommand("", "invalid", "weak"), CancellationToken.None);

        Assert.IsType<RegistrationOutcome.ValidationFailure>(result);
        Assert.Empty(publisher.PublishedEvents);
    }

    [Fact]
    public async Task RegisterAsync_remains_successful_when_event_publication_fails()
    {
        var publisher = new RecordingUserCreatedEventPublisher(publishResult: false);
        var service = new UserRegistrationService(new InMemoryUserRepository(), new FakePasswordHasher(), publisher);

        var result = await service.RegisterAsync(
            new RegisterUserCommand("Alice", "alice@example.com", "Password1!"), CancellationToken.None);

        var success = Assert.IsType<RegistrationOutcome.Success>(result);
        Assert.False(success.EventPublished);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hashed:{password}";

        public bool VerifyPassword(string passwordHash, string providedPassword) => passwordHash == $"hashed:{providedPassword}";
    }

    private sealed class RecordingUserCreatedEventPublisher(
        Action? beforePublish = null,
        bool publishResult = true) : IUserCreatedEventPublisher
    {
        public List<UserCreatedEvent> PublishedEvents { get; } = [];

        public Task<bool> PublishAsync(UserCreatedEvent userCreatedEvent, CancellationToken cancellationToken)
        {
            beforePublish?.Invoke();
            PublishedEvents.Add(userCreatedEvent);
            return Task.FromResult(publishResult);
        }
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

        public bool ChangesSaved { get; private set; }

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
            ChangesSaved = true;
            return Task.FromResult(1);
        }
    }
}
