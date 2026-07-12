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

    public async Task<ImeProcessResult> ProcessKeyAsync(ImeKey key)
    {
        var response = await ProcessKeyRequestAsync(new ImeProcessKeyRequest(key)).ConfigureAwait(false);

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

    public async Task<ImeSessionSnapshot> GetSnapshotAsync()
    {
        var response = await SendWithRetryAsync(proxy => proxy.GetResponseAsync<ImeSnapshotResponse>(
                XiaoXiImeIpcRoutes.GetSnapshot,
                new ImeSnapshotRequest())).ConfigureAwait(false);

        return response?.Snapshot ?? throw new InvalidOperationException("IME IPC server returned an empty snapshot response.");
    }

    public async Task<ImeUiState> GetUiStateAsync()
    {
        var response = await SendWithRetryAsync(proxy => proxy.GetResponseAsync<ImeUiStateResponse>(
                XiaoXiImeIpcRoutes.GetUiState,
                new ImeUiStateRequest())).ConfigureAwait(false);

        return response?.UiState ?? throw new InvalidOperationException("IME IPC server returned an empty UI-state response.");
    }

    public async Task<ImeHostStatus> GetHostStatusAsync()
    {
        var response = await SendWithRetryAsync(proxy => proxy.GetResponseAsync<ImeHostStatusResponse>(
                XiaoXiImeIpcRoutes.GetHostStatus,
                new ImeHostStatusRequest())).ConfigureAwait(false);

        return response?.Status ?? throw new InvalidOperationException("IME IPC server returned an empty host-status response.");
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
