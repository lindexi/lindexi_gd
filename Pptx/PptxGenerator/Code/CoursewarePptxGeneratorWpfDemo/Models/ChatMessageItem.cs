namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents a chat message displayed in the Copilot panel.
/// </summary>
public sealed class ChatMessageItem
{
    /// <summary>
    /// Gets the sender display name.
    /// </summary>
    public required string Sender { get; init; }

    /// <summary>
    /// Gets the message content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets a value indicating whether the message was sent by the user.
    /// </summary>
    public required bool IsUser { get; init; }
}
