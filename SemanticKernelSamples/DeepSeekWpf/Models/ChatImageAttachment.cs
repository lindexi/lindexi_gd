namespace DeepSeekWpf.Models;

public sealed record ChatImageAttachment(string FileName, string MediaType, byte[] Data)
{
    public long Size => Data.LongLength;
}
