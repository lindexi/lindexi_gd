using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

public sealed class XiaoXiImeIpcClient : IDisposable
{
    private readonly JsonIpcDirectRoutedProvider _provider;
    private readonly XiaoXiImeIpcOptions _options;
    private JsonIpcDirectRoutedClientProxy? _clientProxy;

    public XiaoXiImeIpcClient(XiaoXiImeIpcOptions? options = null)
        : this(XiaoXiImeIpcProviderFactory.CreateClientProvider(), options)
    {
    }

    internal XiaoXiImeIpcClient(JsonIpcDirectRoutedProvider provider, XiaoXiImeIpcOptions? options = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _options = options ?? XiaoXiImeIpcOptions.Default;
    }

    public async Task ConnectAsync()
    {
        if (_clientProxy is not null)
        {
            return;
        }

        _provider.StartServer();
        _clientProxy = await WithTimeout(
            _provider.GetAndConnectClientAsync(_options.ServerName),
            _options.EffectiveConnectTimeout).ConfigureAwait(false);
    }

    public async Task<ImeHostStatus> WaitUntilReadyAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var remaining = timeout ?? XiaoXiImeIpcOptions.DefaultReadyTimeout;
        Exception? lastException = null;

        while (remaining >= TimeSpan.Zero)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var startedAt = DateTime.UtcNow;
            try
            {
                var status = await GetHostStatusAsync().ConfigureAwait(false);
                if (status.IsRunning)
                {
                    return status;
                }

                lastException = new InvalidOperationException("IME host reported that it is not running.");
            }
            catch (Exception exception) when (exception is TimeoutException
                or IOException
                or System.ComponentModel.Win32Exception
                or InvalidOperationException)
            {
                lastException = exception;
                _clientProxy = null;
            }

            if (remaining == TimeSpan.Zero)
            {
                break;
            }

            var delay = TimeSpan.FromMilliseconds(50);
            if (delay > remaining)
            {
                delay = remaining;
            }

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            var elapsed = DateTime.UtcNow - startedAt;
            remaining = elapsed >= remaining ? TimeSpan.Zero : remaining - elapsed;
        }

        throw new TimeoutException(
            lastException is null
                ? "IME host did not become ready before the timeout elapsed."
                : $"IME host did not become ready before the timeout elapsed: {lastException.Message}",
            lastException);
    }

    public Task<ImeProcessResult> ProcessKeyAsync(ImeKey key) =>
        ProcessKeyAsync(key, ImeSessionId.Default);

    public async Task<ImeProcessResult> ProcessKeyAsync(ImeKey key, ImeSessionId sessionId)
    {
        var response = await ProcessKeyRequestAsync(new ImeProcessKeyRequest(key, sessionId)).ConfigureAwait(false);

        return response.Result;
    }

    public async Task<ImeProcessKeyResponse> ProcessKeyRequestAsync(ImeProcessKeyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var response = await SendWithRetryAsync(proxy => proxy.GetResponseAsync<ImeProcessKeyResponse>(
                XiaoXiImeIpcRoutes.ProcessKey,
                request)).ConfigureAwait(false);

        return response ?? throw new InvalidOperationException("IME IPC server returned an empty process-key response.");
    }

    public Task<ImeSessionSnapshot> GetSnapshotAsync() => GetSnapshotAsync(ImeSessionId.Unspecified);

    public async Task<ImeSessionSnapshot> GetSnapshotAsync(ImeSessionId sessionId)
    {
        var response = await SendWithRetryAsync(proxy => proxy.GetResponseAsync<ImeSnapshotResponse>(
                XiaoXiImeIpcRoutes.GetSnapshot,
                new ImeSnapshotRequest(sessionId))).ConfigureAwait(false);

        return response?.Snapshot ?? throw new InvalidOperationException("IME IPC server returned an empty snapshot response.");
    }

    public Task<ImeUiState> GetUiStateAsync() => GetUiStateAsync(ImeSessionId.Unspecified);

    public async Task<ImeUiState> GetUiStateAsync(ImeSessionId sessionId)
    {
        var response = await SendWithRetryAsync(proxy => proxy.GetResponseAsync<ImeUiStateResponse>(
                XiaoXiImeIpcRoutes.GetUiState,
                new ImeUiStateRequest(sessionId))).ConfigureAwait(false);

        return response?.UiState ?? throw new InvalidOperationException("IME IPC server returned an empty UI-state response.");
    }

    public async Task<ImeHostStatus> GetHostStatusAsync()
    {
        var response = await SendWithRetryAsync(proxy => proxy.GetResponseAsync<ImeHostStatusResponse>(
                XiaoXiImeIpcRoutes.GetHostStatus,
                new ImeHostStatusRequest())).ConfigureAwait(false);

        return response?.Status ?? throw new InvalidOperationException("IME IPC server returned an empty host-status response.");
    }

    public Task<ImeResetSessionResponse> ResetSessionAsync(ImeSessionId sessionId) =>
        ResetSessionAsync(new ImeResetSessionRequest(sessionId));

    public Task<ImeResetSessionResponse> ResetAllSessionsAsync() =>
        ResetSessionAsync(new ImeResetSessionRequest(ResetAll: true));

    public async Task<ImeResetSessionResponse> ResetSessionAsync(ImeResetSessionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var response = await SendWithRetryAsync(proxy => proxy.GetResponseAsync<ImeResetSessionResponse>(
                XiaoXiImeIpcRoutes.ResetSession,
                request)).ConfigureAwait(false);

        return response ?? throw new InvalidOperationException("IME IPC server returned an empty reset-session response.");
    }

    public Task<ImeProcessResult> SetCompositionAsync(string composition) =>
        SetCompositionAsync(composition, ImeSessionId.Default);

    public async Task<ImeProcessResult> SetCompositionAsync(string composition, ImeSessionId sessionId)
    {
        var response = await SetCompositionAsync(new ImeSetCompositionRequest(composition, sessionId)).ConfigureAwait(false);
        return response.Result;
    }

    public async Task<ImeSetCompositionResponse> SetCompositionAsync(ImeSetCompositionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var response = await SendWithRetryAsync(proxy => proxy.GetResponseAsync<ImeSetCompositionResponse>(
                XiaoXiImeIpcRoutes.SetComposition,
                request)).ConfigureAwait(false);

        return response ?? throw new InvalidOperationException("IME IPC server returned an empty set-composition response.");
    }

    public Task<ImeCandidate[]> QueryConversionListAsync(string source) =>
        QueryConversionListAsync(source, ImeSessionId.Default);

    public Task<ImeCandidate[]> QueryConversionListAsync(string source, ImeSessionId sessionId) =>
        QueryConversionListAsync(source, sessionId, ImeConversionListKind.Conversion);

    public async Task<ImeCandidate[]> QueryConversionListAsync(
        string source,
        ImeSessionId sessionId,
        ImeConversionListKind kind)
    {
        var response = await QueryConversionListAsync(new ImeConversionListRequest(source, SessionId: sessionId, Kind: kind)).ConfigureAwait(false);
        return response.Candidates;
    }

    public Task<ImeCandidate[]> QueryReverseConversionListAsync(string source) =>
        QueryConversionListAsync(source, ImeSessionId.Default, ImeConversionListKind.ReverseConversion);

    public async Task<ImeConversionListResponse> QueryConversionListAsync(ImeConversionListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var response = await SendWithRetryAsync(proxy => proxy.GetResponseAsync<ImeConversionListResponse>(
                XiaoXiImeIpcRoutes.QueryConversionList,
                request)).ConfigureAwait(false);

        return response ?? throw new InvalidOperationException("IME IPC server returned an empty conversion-list response.");
    }

    public Task<ImeRegisterWordResponse> RegisterWordAsync(string reading, string text) =>
        RegisterWordAsync(new ImeRegisterWordRequest(ImeRegisterWordAction.Register, reading, text));

    public Task<ImeRegisterWordResponse> UnregisterWordAsync(string reading, string text) =>
        RegisterWordAsync(new ImeRegisterWordRequest(ImeRegisterWordAction.Unregister, reading, text));

    public Task<ImeRegisterWordResponse> EnumerateRegisterWordsAsync(string? reading = null, string? text = null) =>
        RegisterWordAsync(new ImeRegisterWordRequest(ImeRegisterWordAction.Enumerate, reading ?? string.Empty, text ?? string.Empty));

    public async Task<ImeRegisterWordResponse> RegisterWordAsync(ImeRegisterWordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var response = await SendWithRetryAsync(proxy => proxy.GetResponseAsync<ImeRegisterWordResponse>(
                XiaoXiImeIpcRoutes.RegisterWord,
                request)).ConfigureAwait(false);

        return response ?? throw new InvalidOperationException("IME IPC server returned an empty register-word response.");
    }

    public void Dispose()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private async Task<JsonIpcDirectRoutedClientProxy> GetClientProxyAsync()
    {
        await ConnectAsync().ConfigureAwait(false);
        return _clientProxy!;
    }

    private async Task<TResponse?> SendWithRetryAsync<TResponse>(Func<JsonIpcDirectRoutedClientProxy, Task<TResponse?>> sendAsync)
    {
        Exception? lastException = null;
        for (var attempt = 0; attempt <= _options.EffectiveRetryCount; attempt++)
        {
            try
            {
                var proxy = await GetClientProxyAsync().ConfigureAwait(false);
                return await WithTimeout(sendAsync(proxy), _options.EffectiveRequestTimeout).ConfigureAwait(false);
            }
            catch (Exception exception) when (attempt < _options.EffectiveRetryCount)
            {
                lastException = exception;
                _clientProxy = null;
            }
        }

        if (lastException is not null)
        {
            throw lastException;
        }

        var finalProxy = await GetClientProxyAsync().ConfigureAwait(false);
        return await WithTimeout(sendAsync(finalProxy), _options.EffectiveRequestTimeout).ConfigureAwait(false);
    }

    private static async Task<T> WithTimeout<T>(Task<T> task, TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            return await task.ConfigureAwait(false);
        }

        var completedTask = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
        if (completedTask != task)
        {
            throw new TimeoutException("IME IPC operation timed out.");
        }

        return await task.ConfigureAwait(false);
    }
}
