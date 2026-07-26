namespace DeepSeekWpf.Models;

public enum AiResponsePart
{
    Thought,
    Content,
}

public sealed record AiResponseChunk(AiResponsePart Part, string Delta);
