using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeHost;

public sealed class ImeHostService : IDisposable
{
    private readonly Func<ImeContext> _createImeContext;
    private readonly Dictionary<ImeSessionId, ImeContext> _imeContexts = [];
    private readonly XiaoXiImeIpcServer _ipcServer;
    private readonly object _syncRoot = new();
    private bool _started;
    private string? _lastError;

    public ImeHostService(XiaoXiImeIpcOptions? options = null)
        : this(() => new ImeContext(InMemoryImeDictionary.CreateDefault()), options)
    {
    }

    public ImeHostService(ImeContext imeContext, XiaoXiImeIpcOptions? options = null)
        : this(CreateSingleContextFactory(imeContext), options)
    {
    }

    internal ImeHostService(Func<ImeContext> createImeContext, XiaoXiImeIpcOptions? options = null)
    {
        _createImeContext = createImeContext ?? throw new ArgumentNullException(nameof(createImeContext));
        _ipcServer = new XiaoXiImeIpcServer(ProcessKeyAsync, GetSnapshotAsync, GetUiStateAsync, GetHostStatusAsync, options);
    }

    public void Start()
    {
        try
        {
            _ipcServer.Start();
            lock (_syncRoot)
            {
                _started = true;
                _lastError = null;
            }
        }
        catch (Exception exception)
        {
            lock (_syncRoot)
            {
                _started = false;
                _lastError = exception.Message;
            }

            throw;
        }
    }

    public Task<ImeProcessKeyResponse> ProcessKeyAsync(ImeProcessKeyRequest request)
    {
        lock (_syncRoot)
        {
            var sessionId = request.EffectiveSessionId;
            if (!_imeContexts.TryGetValue(sessionId, out var imeContext))
            {
                imeContext = _createImeContext();
                _imeContexts.Add(sessionId, imeContext);
            }

            var result = imeContext.ProcessKey(request.Key);
            return Task.FromResult(new ImeProcessKeyResponse(
                result,
                sessionId,
                request.Generation,
                request.SequenceNumber));
        }
    }

    public async Task<ImeProcessResult> ProcessKeyAsync(ImeKey key)
    {
        var response = await ProcessKeyAsync(new ImeProcessKeyRequest(key)).ConfigureAwait(false);
        return response.Result;
    }

    public Task<ImeSessionSnapshot> GetSnapshotAsync()
    {
        lock (_syncRoot)
        {
            return Task.FromResult(GetOrCreateContext(ImeSessionId.Default).Snapshot);
        }
    }

    public Task<ImeUiState> GetUiStateAsync()
    {
        lock (_syncRoot)
        {
            return Task.FromResult(ImeUiState.FromSnapshot(GetOrCreateContext(ImeSessionId.Default).Snapshot));
        }
    }

    public Task<ImeHostStatus> GetHostStatusAsync()
    {
        lock (_syncRoot)
        {
            return Task.FromResult(new ImeHostStatus(_started, _lastError));
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            _started = false;
        }

        _ipcServer.Dispose();
    }

    private ImeContext GetOrCreateContext(ImeSessionId sessionId)
    {
        if (_imeContexts.TryGetValue(sessionId, out var imeContext))
        {
            return imeContext;
        }

        imeContext = _createImeContext();
        _imeContexts.Add(sessionId, imeContext);
        return imeContext;
    }

    private static Func<ImeContext> CreateSingleContextFactory(ImeContext imeContext)
    {
        ArgumentNullException.ThrowIfNull(imeContext);
        var used = false;
        return () =>
        {
            if (used)
            {
                return new ImeContext(InMemoryImeDictionary.CreateDefault());
            }

            used = true;
            return imeContext;
        };
    }
}
