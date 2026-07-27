namespace DeepSeekWpf.Services;

public enum AiChatErrorCategory
{
    Configuration,
    Authentication,
    RateLimit,
    Network,
    Timeout,
    Server,
    Protocol,
    EmptyResponse,
    Storage,
    Unknown,
    Canceled,
}

public sealed class AiChatException : Exception
{
    public AiChatException(
        AiChatErrorCategory category,
        string message,
        string correlationId,
        bool isRetryable,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Category = category;
        CorrelationId = correlationId;
        IsRetryable = isRetryable;
    }

    public AiChatErrorCategory Category { get; }

    public string CorrelationId { get; }

    public bool IsRetryable { get; }
}