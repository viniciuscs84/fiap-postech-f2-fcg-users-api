using System.Text.Json;
using FCG.Application.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FCG.Infrastructure.Messaging;

/// <summary>Publishes user-created events to RabbitMQ.</summary>
public sealed class RabbitMqUserCreatedEventPublisher(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqUserCreatedEventPublisher> logger) : IUserCreatedEventPublisher, IAsyncDisposable
{
    private const string RoutingKey = "user.created";
    private const string QueueName = "notifications.user-created";
    private readonly RabbitMqOptions rabbitMqOptions = options.Value;
    private readonly SemaphoreSlim channelGate = new(1, 1);
    private IConnection? connection;
    private IChannel? channel;

    /// <inheritdoc />
    public async Task<bool> PublishAsync(UserCreatedEvent userCreatedEvent, CancellationToken cancellationToken)
    {
        await channelGate.WaitAsync(cancellationToken);
        try
        {
            await EnsureChannelAsync(cancellationToken);
            var activeChannel = channel ?? throw new InvalidOperationException("RabbitMQ channel was not created.");
            await activeChannel.ExchangeDeclareAsync(
                rabbitMqOptions.Exchange,
                ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);
            await activeChannel.QueueDeclareAsync(
                QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);
            await activeChannel.QueueBindAsync(
                QueueName,
                rabbitMqOptions.Exchange,
                RoutingKey,
                arguments: null,
                cancellationToken: cancellationToken);

            var body = JsonSerializer.SerializeToUtf8Bytes(userCreatedEvent);
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Type = nameof(UserCreatedEvent)
            };

            await activeChannel.BasicPublishAsync(
                rabbitMqOptions.Exchange,
                RoutingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await ResetConnectionAsync();
            logger.LogError(
                exception,
                "UserCreatedEvent publication failed for user {UserId}.",
                userCreatedEvent.UserId);
            return false;
        }
        finally
        {
            channelGate.Release();
        }
    }

    private async Task EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (channel is { IsOpen: true })
            return;

        await ResetConnectionAsync();
        var factory = new ConnectionFactory
        {
            HostName = rabbitMqOptions.HostName,
            Port = rabbitMqOptions.Port,
            UserName = rabbitMqOptions.UserName,
            Password = rabbitMqOptions.Password
        };
        connection = await factory.CreateConnectionAsync(cancellationToken);
        channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    private async Task ResetConnectionAsync()
    {
        if (channel is not null)
            await channel.DisposeAsync();
        if (connection is not null)
            await connection.DisposeAsync();
        channel = null;
        connection = null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await channelGate.WaitAsync();
        try
        {
            await ResetConnectionAsync();
        }
        finally
        {
            channelGate.Release();
            channelGate.Dispose();
        }
    }
}
