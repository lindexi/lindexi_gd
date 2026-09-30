using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeModule;

public sealed class ImeHostBridge : IDisposable
{
    private readonly IImeHostBridgeClient _client;
    private readonly Dictionary<ImeSessionId, ImeContext> _fallbackContexts = [];
    private readonly object _syncRoot = new();
    private readonly Func<ImeContext> _createFallbackContext;
    private string? _lastError;

    public ImeHostBridge(XiaoXiImeIpcOptions? options = null)
        : this(new IpcImeHostBridgeClient(options), CreateFallbackContext())
    {
    }

    internal ImeHostBridge(IImeHostBridgeClient client, ImeContext? fallbackContext = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _createFallbackContext = fallbackContext is null
            ? CreateFallbackContext
            : CreateSingleFallbackFactory(fallbackContext);
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

    public ImeProcessResult ProcessKey(ImeKey key) => ProcessKey(key, ImeSessionId.Default);

    public ImeProcessResult ProcessKey(ImeKey key, ImeSessionId sessionId)
    {
        sessionId = sessionId.Effective;
        try
        {
            var result = _client.ProcessKeyAsync(key, sessionId).ConfigureAwait(false).GetAwaiter().GetResult();
            _lastError = null;
            return result;
        }
        catch (ImeHostUnavailableException exception)
        {
            _lastError = exception.Message;
            return GetFallbackContext(sessionId).ProcessKey(key);
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
            return GetFallbackContext(sessionId.Effective).Snapshot;
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
            return ImeUiState.FromSnapshot(
                GetFallbackContext(sessionId.Effective).Snapshot,
                $"Host unavailable; using the minimal fallback dictionary: {exception.Message}",
                isHostUnavailable: true,
                isUsingFallbackDictionary: false);
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
            ResetFallback(request);
            return response;
        }
        catch (ImeHostUnavailableException exception)
        {
            _lastError = exception.Message;
            ResetFallback(request);
            return new ImeResetSessionResponse(
                request.ResetAll ? ImeSessionId.Unspecified : request.SessionId.Effective,
                true);
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
            var result = GetFallbackContext(request.EffectiveSessionId).SetComposition(request.Composition ?? string.Empty);
            return new ImeSetCompositionResponse(result, request.EffectiveSessionId);
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
            var context = GetFallbackContext(request.EffectiveSessionId);
            var source = request.Source ?? string.Empty;
            var candidates = (request.Kind == ImeConversionListKind.ReverseConversion
                    ? context.QueryReverseConversionList(source, request.MaxCount)
                    : context.QueryConversionList(source, request.MaxCount))
                .ToArray();
            return new ImeConversionListResponse(candidates, request.EffectiveSessionId);
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
            var context = GetFallbackContext(request.EffectiveSessionId);
            var succeeded = request.Action switch
            {
                ImeRegisterWordAction.Register => context.RegisterWord(request.Reading ?? string.Empty, request.Text ?? string.Empty),
                ImeRegisterWordAction.Unregister => context.UnregisterWord(request.Reading ?? string.Empty, request.Text ?? string.Empty),
                ImeRegisterWordAction.Enumerate => true,
                _ => false,
            };
            var entries = request.Action == ImeRegisterWordAction.Enumerate
                ? context.EnumerateRegisterWords(request.Reading, request.Text)
                    .Select(entry => new ImeRegisterWordEntry(entry.Reading, entry.Text))
                    .ToArray()
                : [];
            return new ImeRegisterWordResponse(succeeded, entries, request.EffectiveSessionId);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    internal interface IImeHostBridgeClient : IDisposable
    {
        Task<ImeProcessResult> ProcessKeyAsync(ImeKey key, ImeSessionId sessionId);

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

    private ImeContext GetFallbackContext(ImeSessionId sessionId)
    {
        lock (_syncRoot)
        {
            if (!_fallbackContexts.TryGetValue(sessionId, out var context))
            {
                context = _createFallbackContext();
                _fallbackContexts.Add(sessionId, context);
            }

            return context;
        }
    }

    private void ResetFallback(ImeResetSessionRequest request)
    {
        lock (_syncRoot)
        {
            if (request.ResetAll)
            {
                _fallbackContexts.Clear();
                return;
            }

            _fallbackContexts.Remove(request.SessionId.Effective);
        }
    }

    private static ImeContext CreateFallbackContext()
    {
        return new ImeContext(new UserDictionary(InMemoryImeDictionary.CreateMinimalFallback()));
    }

    private static Func<ImeContext> CreateSingleFallbackFactory(ImeContext fallbackContext)
    {
        var used = false;
        return () =>
        {
            if (used)
            {
                return CreateFallbackContext();
            }

            used = true;
            return fallbackContext;
        };
    }

    private sealed class IpcImeHostBridgeClient : IImeHostBridgeClient
    {
        private readonly XiaoXiImeIpcClient _client;

        public IpcImeHostBridgeClient(XiaoXiImeIpcOptions? options)
        {
            _client = new XiaoXiImeIpcClient(options);
        }

        public Task<ImeProcessResult> ProcessKeyAsync(ImeKey key, ImeSessionId sessionId)
        {
            return InvokeAsync(() => _client.ProcessKeyAsync(key, sessionId));
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
