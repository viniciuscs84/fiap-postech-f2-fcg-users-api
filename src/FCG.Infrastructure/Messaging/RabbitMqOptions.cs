namespace FCG.Infrastructure.Messaging;

/// <summary>RabbitMQ connection and exchange configuration.</summary>
public sealed class RabbitMqOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "RabbitMq";

    /// <summary>Broker host name.</summary>
    public string HostName { get; init; } = "localhost";

    /// <summary>AMQP port.</summary>
    public int Port { get; init; } = 5672;

    /// <summary>Broker user name.</summary>
    public string UserName { get; init; } = "guest";

    /// <summary>Broker password.</summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>Direct integration-event exchange.</summary>
    public string Exchange { get; init; } = "fcg.events";
}
