using XiaoXiIme.Foundation;
using XiaoXiIme.Dictionary;
using XiaoXiIme.ImeCore;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeModule;

public sealed class ImeHostBridge : IDisposable
{
    private readonly IImeHostBridgeClient _client;
    private readonly ImeContext _fallbackContext;
    private string? _lastError;

    public ImeHostBridge(XiaoXiImeIpcOptions? options = null)
        : this(new IpcImeHostBridgeClient(options), new ImeContext(InMemoryImeDictionary.CreateDefault()))
    {
    }

    internal ImeHostBridge(IImeHostBridgeClient client, ImeContext? fallbackContext = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _fallbackContext = fallbackContext ?? new ImeContext(InMemoryImeDictionary.CreateDefault());
    }

    public string? LastError => _lastError;

    public ImeProcessResult ProcessKey(ImeKey key)
    {
        try
        {
            var result = _client.ProcessKeyAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
            _lastError = null;
            return result;
        }
        catch (Exception exception)
        {
            _lastError = exception.Message;
            return _fallbackContext.ProcessKey(key);
        }
    }

    public ImeSessionSnapshot GetSnapshot()
    {
        try
        {
            var snapshot = _client.GetSnapshotAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            _lastError = null;
            return snapshot;
        }
        catch (Exception exception)
        {
            _lastError = exception.Message;
            return _fallbackContext.Snapshot;
        }
    }

    public ImeUiState GetUiState()
    {
        try
        {
            var uiState = _client.GetUiStateAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            _lastError = null;
            return uiState;
        }
        catch (Exception exception)
        {
            _lastError = exception.Message;
            return ImeUiState.FromSnapshot(_fallbackContext.Snapshot);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    internal interface IImeHostBridgeClient : IDisposable
    {
        Task<ImeProcessResult> ProcessKeyAsync(ImeKey key);

        Task<ImeSessionSnapshot> GetSnapshotAsync();

        Task<ImeUiState> GetUiStateAsync();
    }

    private sealed class IpcImeHostBridgeClient : IImeHostBridgeClient
    {
        private readonly XiaoXiImeIpcClient _client;

        public IpcImeHostBridgeClient(XiaoXiImeIpcOptions? options)
        {
            _client = new XiaoXiImeIpcClient(options);
        }

        public Task<ImeProcessResult> ProcessKeyAsync(ImeKey key)
        {
            return _client.ProcessKeyAsync(key);
        }

        public Task<ImeSessionSnapshot> GetSnapshotAsync()
        {
            return _client.GetSnapshotAsync();
        }

        public Task<ImeUiState> GetUiStateAsync()
        {
            return _client.GetUiStateAsync();
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
