using System.Text;
using System.Text.Json.Nodes;

namespace WayellejecalqallWafaykaiwe;

public class DeepSeekResponse : IDisposable, IAsyncDisposable
{
    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public Stream ResponseStream { get; init; } = null!;

    internal IDisposable? DisposableObject { get; init; }

    public async Task<string> ReadToEndAsync()
    {
        using var streamReader = new StreamReader(ResponseStream, leaveOpen: true);
        return await streamReader.ReadToEndAsync();
    }

    public async Task<string> ReadAsResponseText()
    {
        var stringBuilder = new StringBuilder();
        using var streamReader = new StreamReader(ResponseStream, leaveOpen: true);
        while (true)
        {
            var line = await streamReader.ReadLineAsync();
            if (line is null)
            {
                break;
            }

            var value = JsonNode.Parse(line)?["response"]?.ToString();
            if (!string.IsNullOrEmpty(value))
            {
                stringBuilder.Append(value);
            }
        }

        return stringBuilder.ToString();
    }

    public async IAsyncEnumerable<char> ReadAsync()
    {
        var buffer = new char[1];
        using var streamReader = new StreamReader(ResponseStream, leaveOpen: true);

        while ((await streamReader.ReadBlockAsync(buffer, 0, 1)) > 0)
        {
            yield return buffer[0];
        }
    }

    public void Dispose()
    {
        ResponseStream.Dispose();
        DisposableObject?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await ResponseStream.DisposeAsync();
        DisposableObject?.Dispose();
    }
}