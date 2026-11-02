namespace FCG.Application.Users;

/// <summary>Integration event emitted after a user is persisted successfully.</summary>
public sealed record UserCreatedEvent(Guid UserId, string Email);

/// <summary>Publishes the user-created integration event.</summary>
public interface IUserCreatedEventPublisher
{
    /// <summary>Publishes the event and reports whether the publish attempt completed.</summary>
    Task<bool> PublishAsync(UserCreatedEvent userCreatedEvent, CancellationToken cancellationToken);
}
