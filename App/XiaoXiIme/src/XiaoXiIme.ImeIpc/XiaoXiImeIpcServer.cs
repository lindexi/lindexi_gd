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
        Func<ImeSnapshotRequest, Task<ImeSessionSnapshot>> getSnapshotAsync,
        Func<ImeUiStateRequest, Task<ImeUiState>>? getUiStateAsync = null,
        Func<Task<ImeHostStatus>>? getHostStatusAsync = null,
        Func<ImeResetSessionRequest, Task<ImeResetSessionResponse>>? resetSessionAsync = null,
        Func<ImeSetCompositionRequest, Task<ImeSetCompositionResponse>>? setCompositionAsync = null,
        Func<ImeConversionListRequest, Task<ImeConversionListResponse>>? queryConversionListAsync = null,
        Func<ImeRegisterWordRequest, Task<ImeRegisterWordResponse>>? registerWordAsync = null,
        XiaoXiImeIpcOptions? options = null)
        : this(XiaoXiImeIpcProviderFactory.CreateServerProvider(options), processKeyAsync, getSnapshotAsync, getUiStateAsync, getHostStatusAsync, resetSessionAsync, setCompositionAsync, queryConversionListAsync, registerWordAsync)
    {
    }

    internal XiaoXiImeIpcServer(
        JsonIpcDirectRoutedProvider provider,
        Func<ImeProcessKeyRequest, Task<ImeProcessKeyResponse>> processKeyAsync,
        Func<ImeSnapshotRequest, Task<ImeSessionSnapshot>> getSnapshotAsync,
        Func<ImeUiStateRequest, Task<ImeUiState>>? getUiStateAsync = null,
        Func<Task<ImeHostStatus>>? getHostStatusAsync = null,
        Func<ImeResetSessionRequest, Task<ImeResetSessionResponse>>? resetSessionAsync = null,
        Func<ImeSetCompositionRequest, Task<ImeSetCompositionResponse>>? setCompositionAsync = null,
        Func<ImeConversionListRequest, Task<ImeConversionListResponse>>? queryConversionListAsync = null,
        Func<ImeRegisterWordRequest, Task<ImeRegisterWordResponse>>? registerWordAsync = null)
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
            async request => new ImeSnapshotResponse(await SafeSnapshotAsync(getSnapshotAsync, request).ConfigureAwait(false)));

        _provider.AddRequestHandler<ImeUiStateRequest, ImeUiStateResponse>(
            XiaoXiImeIpcRoutes.GetUiState,
            async request => new ImeUiStateResponse(await SafeUiStateAsync(getUiStateAsync, getSnapshotAsync, request).ConfigureAwait(false)));

        _provider.AddRequestHandler<ImeHostStatusRequest, ImeHostStatusResponse>(
            XiaoXiImeIpcRoutes.GetHostStatus,
            async _ => new ImeHostStatusResponse(await SafeHostStatusAsync().ConfigureAwait(false)));

        _provider.AddRequestHandler<ImeResetSessionRequest, ImeResetSessionResponse>(
            XiaoXiImeIpcRoutes.ResetSession,
            request => SafeResetSessionAsync(resetSessionAsync, request));

        _provider.AddRequestHandler<ImeSetCompositionRequest, ImeSetCompositionResponse>(
            XiaoXiImeIpcRoutes.SetComposition,
            request => SafeSetCompositionAsync(setCompositionAsync, request));

        _provider.AddRequestHandler<ImeConversionListRequest, ImeConversionListResponse>(
            XiaoXiImeIpcRoutes.QueryConversionList,
            request => SafeQueryConversionListAsync(queryConversionListAsync, request));

        _provider.AddRequestHandler<ImeRegisterWordRequest, ImeRegisterWordResponse>(
            XiaoXiImeIpcRoutes.RegisterWord,
            request => SafeRegisterWordAsync(registerWordAsync, request));
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

    private static async Task<ImeSessionSnapshot> SafeSnapshotAsync(
        Func<ImeSnapshotRequest, Task<ImeSessionSnapshot>> getSnapshotAsync,
        ImeSnapshotRequest request)
    {
        try
        {
            return await getSnapshotAsync(request ?? new ImeSnapshotRequest()).ConfigureAwait(false);
        }
        catch
        {
            return ImeSessionSnapshot.Empty;
        }
    }

    private static async Task<ImeUiState> SafeUiStateAsync(
        Func<ImeUiStateRequest, Task<ImeUiState>>? getUiStateAsync,
        Func<ImeSnapshotRequest, Task<ImeSessionSnapshot>> getSnapshotAsync,
        ImeUiStateRequest request)
    {
        request ??= new ImeUiStateRequest();
        try
        {
            if (getUiStateAsync is not null)
            {
                return await getUiStateAsync(request).ConfigureAwait(false);
            }

            return ImeUiState.FromSnapshot(await getSnapshotAsync(new ImeSnapshotRequest(request.SessionId)).ConfigureAwait(false));
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

    private static async Task<ImeResetSessionResponse> SafeResetSessionAsync(
        Func<ImeResetSessionRequest, Task<ImeResetSessionResponse>>? resetSessionAsync,
        ImeResetSessionRequest request)
    {
        request ??= new ImeResetSessionRequest();
        if (resetSessionAsync is null)
        {
            return new ImeResetSessionResponse(request.SessionId, false);
        }

        try
        {
            return await resetSessionAsync(request).ConfigureAwait(false);
        }
        catch
        {
            return new ImeResetSessionResponse(request.SessionId, false);
        }
    }

    private static async Task<ImeSetCompositionResponse> SafeSetCompositionAsync(
        Func<ImeSetCompositionRequest, Task<ImeSetCompositionResponse>>? setCompositionAsync,
        ImeSetCompositionRequest request)
    {
        request ??= new ImeSetCompositionRequest();
        if (setCompositionAsync is null)
        {
            return new ImeSetCompositionResponse(
                new ImeProcessResult(ImeSessionSnapshot.Empty, null, false),
                request.EffectiveSessionId);
        }

        try
        {
            return await setCompositionAsync(request).ConfigureAwait(false);
        }
        catch
        {
            return new ImeSetCompositionResponse(
                new ImeProcessResult(ImeSessionSnapshot.Empty, null, false),
                request.EffectiveSessionId);
        }
    }

    private static async Task<ImeConversionListResponse> SafeQueryConversionListAsync(
        Func<ImeConversionListRequest, Task<ImeConversionListResponse>>? queryConversionListAsync,
        ImeConversionListRequest request)
    {
        request ??= new ImeConversionListRequest();
        if (queryConversionListAsync is null)
        {
            return new ImeConversionListResponse([], request.EffectiveSessionId);
        }

        try
        {
            return await queryConversionListAsync(request).ConfigureAwait(false);
        }
        catch
        {
            return new ImeConversionListResponse([], request.EffectiveSessionId);
        }
    }

    private static async Task<ImeRegisterWordResponse> SafeRegisterWordAsync(
        Func<ImeRegisterWordRequest, Task<ImeRegisterWordResponse>>? registerWordAsync,
        ImeRegisterWordRequest request)
    {
        request ??= new ImeRegisterWordRequest();
        if (registerWordAsync is null)
        {
            return new ImeRegisterWordResponse(false, [], request.EffectiveSessionId);
        }

        try
        {
            return await registerWordAsync(request).ConfigureAwait(false);
        }
        catch
        {
            return new ImeRegisterWordResponse(false, [], request.EffectiveSessionId);
        }
    }
}
