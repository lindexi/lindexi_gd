using XiaoXiIme.Foundation;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeModule;

public sealed class ImeHostBridge : IDisposable
{
    private readonly IImeHostBridgeClient _client;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ImeSessionId, long> _generations = new();
    private string? _lastError;

    public ImeHostBridge(XiaoXiImeIpcOptions? options = null)
        : this(new IpcImeHostBridgeClient(options))
    {
    }

    internal ImeHostBridge(IImeHostBridgeClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public string? LastError => _lastError;

    public bool IsUsingFallback => _lastError is not null;

    internal static bool IsHostUnavailable(Exception exception)
    {
        if (exception is TimeoutException
            or IOException
            or System.ComponentModel.Win32Exception
            or NotSupportedException)
        {
            return true;
        }

        if (exception is not InvalidOperationException invalidOperation)
        {
            return false;
        }

        if (invalidOperation.InnerException is TimeoutException
            or IOException
            or System.ComponentModel.Win32Exception
            or NotSupportedException)
        {
            return true;
        }

        var message = invalidOperation.Message;
        return message.Contains("timed out", StringComparison.OrdinalIgnoreCase)
               || message.Contains("empty process-key response", StringComparison.OrdinalIgnoreCase)
               || message.Contains("empty snapshot response", StringComparison.OrdinalIgnoreCase)
               || message.Contains("empty UI-state response", StringComparison.OrdinalIgnoreCase)
               || message.Contains("empty host-status response", StringComparison.OrdinalIgnoreCase)
               || message.Contains("empty reset-session response", StringComparison.OrdinalIgnoreCase)
               || message.Contains("empty set-composition response", StringComparison.OrdinalIgnoreCase)
               || message.Contains("empty conversion-list response", StringComparison.OrdinalIgnoreCase)
               || message.Contains("empty register-word response", StringComparison.OrdinalIgnoreCase)
               || message.Contains("Unable to connect", StringComparison.OrdinalIgnoreCase)
               || message.Contains("connect", StringComparison.OrdinalIgnoreCase);
    }

    internal void UpdateInputTarget(ImeInputTargetUpdate request)
    {
        try
        {
            _client.UpdateInputTargetAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            _lastError = null;
        }
        catch (ImeHostUnavailableException exception)
        {
            _lastError = exception.Message;
        }
    }

    public ImeProcessResult ProcessKey(ImeKey key) => ProcessKey(key, ImeSessionId.Default);

    public ImeProcessResult ProcessKey(ImeKey key, ImeSessionId sessionId) => ProcessKey(key, sessionId, null);

    /// <summary>Processes a key with an optional immutable candidate position snapshot.</summary>
    public ImeProcessResult ProcessKey(ImeKey key, ImeSessionId sessionId, ImeCandidateAnchor? anchor)
    {
        sessionId = sessionId.Effective;
        var request = new ImeProcessKeyRequest(key, sessionId, _generations.GetValueOrDefault(sessionId), Anchor: anchor);
        try
        {
            var result = _client.ProcessKeyAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            _lastError = null;
            return result;
        }
        catch (ImeHostUnavailableException exception)
        {
            _lastError = exception.Message;
            _generations.AddOrUpdate(sessionId, 1, static (_, generation) => generation + 1);
            return new ImeProcessResult(ImeSessionSnapshot.Empty, null, false);
        }
    }

    public ImeSessionSnapshot GetSnapshot() => GetSnapshot(ImeSessionId.Unspecified);

    public ImeSessionSnapshot GetSnapshot(ImeSessionId sessionId)
    {
        try
        {
            var snapshot = _client.GetSnapshotAsync(sessionId).ConfigureAwait(false).GetAwaiter().GetResult();
            _lastError = null;
            return snapshot;
        }
        catch (ImeHostUnavailableException exception)
        {
            _lastError = exception.Message;
            return ImeSessionSnapshot.Empty;
        }
    }

    public ImeUiState GetUiState() => GetUiState(ImeSessionId.Unspecified);

    public ImeUiState GetUiState(ImeSessionId sessionId)
    {
        try
        {
            var uiState = _client.GetUiStateAsync(sessionId).ConfigureAwait(false).GetAwaiter().GetResult();
            _lastError = null;
            return uiState;
        }
        catch (ImeHostUnavailableException exception)
        {
            _lastError = exception.Message;
            return ImeUiState.FromSnapshot
            (
                ImeSessionSnapshot.Empty,
                $"Host unavailable: {exception.Message}",
                isHostUnavailable: true,
                isUsingFallbackDictionary: false
            );
        }
    }

    public ImeResetSessionResponse ResetSession(ImeSessionId sessionId) =>
        ResetSession(new ImeResetSessionRequest(sessionId));

    public ImeResetSessionResponse ResetAllSessions() =>
        ResetSession(new ImeResetSessionRequest(ResetAll: true));

    public ImeResetSessionResponse ResetSession(ImeResetSessionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var response = _client.ResetSessionAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            _lastError = null;
            return response;
        }
        catch (ImeHostUnavailableException exception)
        {
            _lastError = exception.Message;
            return new ImeResetSessionResponse
            (
                request.ResetAll ? ImeSessionId.Unspecified : request.SessionId.Effective,
                true
            );
        }
    }

    public ImeProcessResult SetComposition(string composition) =>
        SetComposition(composition, ImeSessionId.Default);

    public ImeProcessResult SetComposition(string composition, ImeSessionId sessionId) =>
        SetComposition(new ImeSetCompositionRequest(composition, sessionId)).Result;

    public ImeSetCompositionResponse SetComposition(ImeSetCompositionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var response = _client.SetCompositionAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            _lastError = null;
            return response;
        }
        catch (ImeHostUnavailableException exception)
        {
            _lastError = exception.Message;
            return new ImeSetCompositionResponse(new ImeProcessResult(ImeSessionSnapshot.Empty, null, false), request.EffectiveSessionId);
        }
    }

    public ImeCandidate[] QueryConversionList(string source) =>
        QueryConversionList(source, ImeSessionId.Default);

    public ImeCandidate[] QueryConversionList(string source, ImeSessionId sessionId) =>
        QueryConversionList(source, sessionId, ImeConversionListKind.Conversion);

    public ImeCandidate[] QueryConversionList(string source, ImeSessionId sessionId, ImeConversionListKind kind) =>
        QueryConversionList(new ImeConversionListRequest(source, SessionId: sessionId, Kind: kind)).Candidates;

    public ImeCandidate[] QueryReverseConversionList(string source) =>
        QueryConversionList(source, ImeSessionId.Default, ImeConversionListKind.ReverseConversion);

    public ImeConversionListResponse QueryConversionList(ImeConversionListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var response = _client.QueryConversionListAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            _lastError = null;
            return response;
        }
        catch (ImeHostUnavailableException exception)
        {
            _lastError = exception.Message;
            return new ImeConversionListResponse([], request.EffectiveSessionId);
        }
    }

    public ImeRegisterWordResponse RegisterWord(ImeRegisterWordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var response = _client.RegisterWordAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            _lastError = null;
            return response;
        }
        catch (ImeHostUnavailableException exception)
        {
            _lastError = exception.Message;
            return new ImeRegisterWordResponse(false, [], request.EffectiveSessionId);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    internal interface IImeHostBridgeClient : IDisposable
    {
        Task<ImeInputTargetUpdateResponse> UpdateInputTargetAsync(ImeInputTargetUpdate request) =>
            Task.FromResult(new ImeInputTargetUpdateResponse(false));

        Task<ImeProcessResult> ProcessKeyAsync(ImeKey key, ImeSessionId sessionId);

        Task<ImeProcessResult> ProcessKeyAsync(ImeProcessKeyRequest request) =>
            ProcessKeyAsync(request.Key, request.EffectiveSessionId);

        Task<ImeSessionSnapshot> GetSnapshotAsync(ImeSessionId sessionId);

        Task<ImeUiState> GetUiStateAsync(ImeSessionId sessionId);

        Task<ImeResetSessionResponse> ResetSessionAsync(ImeResetSessionRequest request);

        Task<ImeSetCompositionResponse> SetCompositionAsync(ImeSetCompositionRequest request);

        Task<ImeConversionListResponse> QueryConversionListAsync(ImeConversionListRequest request);

        Task<ImeRegisterWordResponse> RegisterWordAsync(ImeRegisterWordRequest request);
    }

    internal sealed class ImeHostUnavailableException : Exception
    {
        public ImeHostUnavailableException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

    private sealed class IpcImeHostBridgeClient : IImeHostBridgeClient
    {
        private readonly XiaoXiImeIpcClient _client;

        public IpcImeHostBridgeClient(XiaoXiImeIpcOptions? options)
        {
            _client = new XiaoXiImeIpcClient(options);
        }

        public Task<ImeInputTargetUpdateResponse> UpdateInputTargetAsync(ImeInputTargetUpdate request) =>
            InvokeAsync(() => _client.UpdateInputTargetAsync(request));

        public Task<ImeProcessResult> ProcessKeyAsync(ImeKey key, ImeSessionId sessionId)
        {
            return InvokeAsync(() => _client.ProcessKeyAsync(key, sessionId));
        }

        public async Task<ImeProcessResult> ProcessKeyAsync(ImeProcessKeyRequest request)
        {
            var response = await InvokeAsync(() => _client.ProcessKeyRequestAsync(request)).ConfigureAwait(false);
            return response.Result;
        }

        public Task<ImeSessionSnapshot> GetSnapshotAsync(ImeSessionId sessionId)
        {
            return InvokeAsync(() => _client.GetSnapshotAsync(sessionId));
        }

        public Task<ImeUiState> GetUiStateAsync(ImeSessionId sessionId)
        {
            return InvokeAsync(() => _client.GetUiStateAsync(sessionId));
        }

        public Task<ImeResetSessionResponse> ResetSessionAsync(ImeResetSessionRequest request)
        {
            return InvokeAsync(() => _client.ResetSessionAsync(request));
        }

        public Task<ImeSetCompositionResponse> SetCompositionAsync(ImeSetCompositionRequest request)
        {
            return InvokeAsync(() => _client.SetCompositionAsync(request));
        }

        public Task<ImeConversionListResponse> QueryConversionListAsync(ImeConversionListRequest request)
        {
            return InvokeAsync(() => _client.QueryConversionListAsync(request));
        }

        public Task<ImeRegisterWordResponse> RegisterWordAsync(ImeRegisterWordRequest request)
        {
            return InvokeAsync(() => _client.RegisterWordAsync(request));
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private static async Task<T> InvokeAsync<T>(Func<Task<T>> operation)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (Exception exception) when (ImeHostBridge.IsHostUnavailable(exception))
            {
                throw new ImeHostUnavailableException(exception.Message, exception);
            }
        }
    }
}