namespace FCG.Application.Authentication;

/// <summary>Represents the result of an authentication attempt.</summary>
public abstract record LoginOutcome
{
    private LoginOutcome()
    {
    }

    /// <summary>Indicates successful authentication.</summary>
    public sealed record Success(string AccessToken) : LoginOutcome;

    /// <summary>Indicates invalid or unknown credentials.</summary>
    public sealed record Failure : LoginOutcome;
}
