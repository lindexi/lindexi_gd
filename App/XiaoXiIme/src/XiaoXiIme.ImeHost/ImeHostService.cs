using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeHost;

public sealed class ImeHostService : IDisposable
{
    private readonly ImeContext _imeContext;
    private readonly XiaoXiImeIpcServer _ipcServer;
    private readonly object _syncRoot = new();
    private bool _started;
    private string? _lastError;

    public ImeHostService(XiaoXiImeIpcOptions? options = null)
        : this(new ImeContext(InMemoryImeDictionary.CreateDefault()), options)
    {
    }

    public ImeHostService(ImeContext imeContext, XiaoXiImeIpcOptions? options = null)
    {
        _imeContext = imeContext ?? throw new ArgumentNullException(nameof(imeContext));
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

    public Task<ImeProcessResult> ProcessKeyAsync(ImeKey key)
    {
        lock (_syncRoot)
        {
            return Task.FromResult(_imeContext.ProcessKey(key));
        }
    }

    public Task<ImeSessionSnapshot> GetSnapshotAsync()
    {
        lock (_syncRoot)
        {
            return Task.FromResult(_imeContext.Snapshot);
        }
    }

    public Task<ImeUiState> GetUiStateAsync()
    {
        lock (_syncRoot)
        {
            return Task.FromResult(ImeUiState.FromSnapshot(_imeContext.Snapshot));
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
}
