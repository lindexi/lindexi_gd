using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

public sealed class ImeIpcSession
{
    private readonly XiaoXiImeIpcClient _client;
    private long _sequenceNumber;

    public ImeIpcSession(XiaoXiImeIpcClient client, ImeSessionId sessionId, long generation = 0)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        SessionId = string.IsNullOrWhiteSpace(sessionId.Value) ? ImeSessionId.Create() : sessionId;
        Generation = generation;
    }

    public ImeSessionId SessionId { get; }

    public long Generation { get; private set; }

    public long LastAppliedSequence { get; private set; }

    public async Task<ImeProcessKeyResponse?> ProcessKeyAsync(ImeKey key)
    {
        var sequenceNumber = Interlocked.Increment(ref _sequenceNumber);
        var response = await _client.ProcessKeyRequestAsync(
            new ImeProcessKeyRequest(key, SessionId, Generation, sequenceNumber)).ConfigureAwait(false);

        if (response.SessionId != SessionId || response.Generation != Generation ||
            response.ProcessedThroughSequence < LastAppliedSequence)
        {
            return null;
        }

        LastAppliedSequence = response.ProcessedThroughSequence;
        return response;
    }

    public void Reset()
    {
        Generation++;
        Interlocked.Exchange(ref _sequenceNumber, 0);
        LastAppliedSequence = 0;
    }
}