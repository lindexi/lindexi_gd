using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

public sealed class XiaoXiImeIpcServer : IDisposable
{
    private readonly JsonIpcDirectRoutedProvider _provider;
    private readonly Func<Task<ImeHostStatus>> _getHostStatusAsync;
    private bool _started;

    public XiaoXiImeIpcServer(
        Func<ImeProcessKeyRequest, Task<ImeProcessKeyResponse>> processKeyAsync,
        Func<Task<ImeSessionSnapshot>> getSnapshotAsync,
        Func<Task<ImeUiState>>? getUiStateAsync = null,
        Func<Task<ImeHostStatus>>? getHostStatusAsync = null,
        XiaoXiImeIpcOptions? options = null)
        : this(XiaoXiImeIpcProviderFactory.CreateServerProvider(options), processKeyAsync, getSnapshotAsync, getUiStateAsync, getHostStatusAsync)
    {
    }

    internal XiaoXiImeIpcServer(
        JsonIpcDirectRoutedProvider provider,
        Func<ImeProcessKeyRequest, Task<ImeProcessKeyResponse>> processKeyAsync,
        Func<Task<ImeSessionSnapshot>> getSnapshotAsync,
        Func<Task<ImeUiState>>? getUiStateAsync = null,
        Func<Task<ImeHostStatus>>? getHostStatusAsync = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        ArgumentNullException.ThrowIfNull(processKeyAsync);
        ArgumentNullException.ThrowIfNull(getSnapshotAsync);
        _getHostStatusAsync = getHostStatusAsync ?? (() => Task.FromResult(_started ? ImeHostStatus.Running : ImeHostStatus.Stopped));

        _provider.AddRequestHandler<ImeProcessKeyRequest, ImeProcessKeyResponse>(
            XiaoXiImeIpcRoutes.ProcessKey,
            request => SafeProcessKeyAsync(processKeyAsync, request));

        _provider.AddRequestHandler<ImeSnapshotRequest, ImeSnapshotResponse>(
            XiaoXiImeIpcRoutes.GetSnapshot,
            async _ => new ImeSnapshotResponse(await SafeSnapshotAsync(getSnapshotAsync).ConfigureAwait(false)));

        _provider.AddRequestHandler<ImeUiStateRequest, ImeUiStateResponse>(
            XiaoXiImeIpcRoutes.GetUiState,
            async _ => new ImeUiStateResponse(await SafeUiStateAsync(getUiStateAsync, getSnapshotAsync).ConfigureAwait(false)));

        _provider.AddRequestHandler<ImeHostStatusRequest, ImeHostStatusResponse>(
            XiaoXiImeIpcRoutes.GetHostStatus,
            async _ => new ImeHostStatusResponse(await SafeHostStatusAsync().ConfigureAwait(false)));
    }

    public void Start()
    {
        if (_started)
        {
            return;
        }

        _provider.StartServer();
        _started = true;
    }

    public void Dispose()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private static async Task<ImeProcessKeyResponse> SafeProcessKeyAsync(
        Func<ImeProcessKeyRequest, Task<ImeProcessKeyResponse>> processKeyAsync,
        ImeProcessKeyRequest request)
    {
        try
        {
            return await processKeyAsync(request).ConfigureAwait(false);
        }
        catch
        {
            return new ImeProcessKeyResponse(
                new ImeProcessResult(ImeSessionSnapshot.Empty, null, false),
                request.EffectiveSessionId,
                request.Generation,
                request.SequenceNumber);
        }
    }

    private static async Task<ImeSessionSnapshot> SafeSnapshotAsync(Func<Task<ImeSessionSnapshot>> getSnapshotAsync)
    {
        try
        {
            return await getSnapshotAsync().ConfigureAwait(false);
        }
        catch
        {
            return ImeSessionSnapshot.Empty;
        }
    }

    private static async Task<ImeUiState> SafeUiStateAsync(Func<Task<ImeUiState>>? getUiStateAsync, Func<Task<ImeSessionSnapshot>> getSnapshotAsync)
    {
        try
        {
            if (getUiStateAsync is not null)
            {
                return await getUiStateAsync().ConfigureAwait(false);
            }

            return ImeUiState.FromSnapshot(await getSnapshotAsync().ConfigureAwait(false));
        }
        catch
        {
            return ImeUiState.Empty;
        }
    }

    private async Task<ImeHostStatus> SafeHostStatusAsync()
    {
        try
        {
            return await _getHostStatusAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return new ImeHostStatus(_started, exception.Message);
        }
    }
}
