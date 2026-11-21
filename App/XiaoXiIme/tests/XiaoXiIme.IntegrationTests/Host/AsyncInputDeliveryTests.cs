using System.Threading.Channels;
using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.IntegrationTests;

public class AsyncInputDeliveryTests
{
    [Fact]
    public async Task KeyboardSelectionArrivesOnResultRoute()
    {
        using var session = new Session();
        await session.SendAsync(ImeKey.FromCharacter('n'));
        await session.SendAsync(ImeKey.FromCharacter('i'));

        var result = await session.SendAsync(new ImeKey(ImeKeyKind.Space));

        Assert.Equal("你", result.Result.CommitText);
    }

    [Fact]
    public async Task PointerSelectionArrivesWithoutAnotherKey()
    {
        using var session = new Session();
        await session.SendAsync(ImeKey.FromCharacter('n'));
        await session.SendAsync(ImeKey.FromCharacter('i'));
        var state = await session.Host.GetUiStateAsync(session.Id);

        session.Host.SelectDisplayedCandidate(session.Id, state.CandidateRevision, 0);
        var result = await session.ReceiveAsync();

        Assert.Equal("你", result.Result.CommitText);
    }

    [Fact]
    public async Task StalePointerSelectionDoesNotCommit()
    {
        using var session = new Session();
        await session.SendAsync(ImeKey.FromCharacter('n'));
        await session.SendAsync(ImeKey.FromCharacter('i'));
        var state = await session.Host.GetUiStateAsync(session.Id);
        await session.SendAsync(new ImeKey(ImeKeyKind.Backspace));

        session.Host.SelectDisplayedCandidate(session.Id, state.CandidateRevision, 0);
        var result = await session.SendAsync(ImeKey.FromCharacter('i'));

        Assert.Equal("ni", result.Result.Snapshot.Composition.Reading);
    }

    private sealed class Session : IDisposable
    {
        private readonly Channel<ImeProcessKeyResponse> _results = Channel.CreateUnbounded<ImeProcessKeyResponse>();
        private readonly string _replyPipe = $"XiaoXiIme_TestReply_{Guid.NewGuid():N}";
        private readonly XiaoXiImeIpcClient _client;
        private long _sequence;

        internal ImeSessionId Id { get; } = new("test-session");
        internal ImeHostService Host { get; }

        internal Session()
        {
            var lexicon = new ImeLexicon([new("你", "ni", 100)], [], ImeProjectionDefinition.FullPinyin, 1);
            var options = new XiaoXiImeIpcOptions($"XiaoXiIme_TestHost_{Guid.NewGuid():N}");
            Host = new ImeHostService(new ImeContext(new InMemoryImeDictionary(lexicon)), options);
            Host.Start();
            _client = new XiaoXiImeIpcClient(_replyPipe, response => _results.Writer.TryWrite(response), options);
            var anchor = new ImeCandidateAnchor(Id, 1, 1, 1, 0, 0, 0, 0, 0, 96, "test", true);
            Host.UpdateInputTargetAsync(new ImeInputTargetUpdate("owner", 1, Id, "context", true, anchor)).GetAwaiter().GetResult();
        }

        internal async Task<ImeProcessKeyResponse> SendAsync(ImeKey key)
        {
            await _client.SendInputAsync(new(_replyPipe, new(key, Id, Generation: 1, SequenceNumber: ++_sequence)));
            return await ReceiveAsync();
        }

        internal async Task<ImeProcessKeyResponse> ReceiveAsync()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            return await _results.Reader.ReadAsync(timeout.Token);
        }

        public void Dispose()
        {
            Host.Dispose();
            _client.Dispose();
        }
    }
}
