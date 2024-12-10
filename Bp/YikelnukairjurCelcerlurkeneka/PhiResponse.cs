namespace YikelnukairjurCelcerlurkeneka;

public class PhiResponse : IDisposable, IAsyncDisposable
{
    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public Stream ResponseStream { get; init; } = null!;

    public async Task<string> ReadToEndAsync()
    {
        using var streamReader = new StreamReader(ResponseStream, leaveOpen: true);
        return await streamReader.ReadToEndAsync();
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
    }

    public async ValueTask DisposeAsync()
    {
        await ResponseStream.DisposeAsync();
    }
}