using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeHost;

public sealed class ImeHostService : IDisposable
{
    private readonly Func<ImeContext> _createImeContext;
    private readonly string? _dictionaryInputScheme;
    private readonly string? _dictionaryLoadError;
    private readonly string? _dictionaryPackagePath;
    private readonly Dictionary<ImeSessionId, ImeContext> _imeContexts = [];
    private readonly XiaoXiImeIpcServer _ipcServer;
    private readonly bool _isUsingFallbackDictionary;
    private readonly string? _isolatedUserDictionaryPath;
    private readonly string? _userDictionaryLoadError;
    private readonly string? _userDictionaryPath;
    private readonly UserDictionary? _userDictionary;
    private readonly UserDictionarySaveCoordinator? _userDictionarySaveCoordinator;
    private readonly object _syncRoot = new();
    private bool _started;
    private string? _lastError;
    private string? _userDictionarySaveError;
    private ImeSessionId _lastActiveSessionId = ImeSessionId.Default;

    /// <param name="hostBaseDirectory">
    /// Optional installed host directory used to resolve the default full Pinyin or Xiaohe package layout.
    /// Explicit <paramref name="dictionaryPackagePath"/> values still take precedence.
    /// </param>
    public ImeHostService(
        XiaoXiImeIpcOptions? options = null,
        string? dictionaryPackagePath = null,
        string? userDictionaryPath = null,
        string? inputScheme = null,
        string? hostBaseDirectory = null)
        : this(LoadRuntimeDictionary(dictionaryPackagePath, userDictionaryPath, inputScheme, hostBaseDirectory), options)
    {
    }

    public ImeHostService(ImeContext imeContext, XiaoXiImeIpcOptions? options = null)
        : this(CreateSingleContextFactory(imeContext), options)
    {
    }

    internal ImeHostService(Func<ImeContext> createImeContext, XiaoXiImeIpcOptions? options = null)
        : this(createImeContext, options, dictionaryPackagePath: null, dictionaryLoadError: null, isUsingFallbackDictionary: false)
    {
    }

    private ImeHostService(RuntimeDictionary runtimeDictionary, XiaoXiImeIpcOptions? options)
    {
        var userDictionary = new UserDictionary(runtimeDictionary.Dictionary, runtimeDictionary.UserDictionaryLoad.Entries);
        _userDictionary = userDictionary;
        _dictionaryPackagePath = runtimeDictionary.PackagePath;
        _dictionaryInputScheme = runtimeDictionary.InputScheme;
        _dictionaryLoadError = runtimeDictionary.LoadError;
        _isUsingFallbackDictionary = runtimeDictionary.IsFallback;
        _userDictionaryPath = runtimeDictionary.UserDictionaryPath;
        _userDictionaryLoadError = runtimeDictionary.UserDictionaryLoad.Error;
        _isolatedUserDictionaryPath = runtimeDictionary.UserDictionaryLoad.IsolatedCorruptFilePath;
        _userDictionarySaveCoordinator = new UserDictionarySaveCoordinator(
            runtimeDictionary.UserDictionaryPath,
            error =>
            {
                lock (_syncRoot)
                {
                    _userDictionarySaveError = error;
                }
            });
        _createImeContext = () => new ImeContext(
            userDictionary,
            learning =>
            {
                userDictionary.Learn(learning);
                _userDictionarySaveCoordinator.RequestSave(userDictionary.GetEntries());
            });
        _ipcServer = new XiaoXiImeIpcServer(
            ProcessKeyAsync,
            GetSnapshotAsync,
            GetUiStateAsync,
            GetHostStatusAsync,
            ResetSessionAsync,
            SetCompositionAsync,
            QueryConversionListAsync,
            RegisterWordAsync,
            options);
    }

    private ImeHostService(
        Func<ImeContext> createImeContext,
        XiaoXiImeIpcOptions? options,
        string? dictionaryPackagePath,
        string? dictionaryLoadError,
        bool isUsingFallbackDictionary)
    {
        _createImeContext = createImeContext ?? throw new ArgumentNullException(nameof(createImeContext));
        _dictionaryPackagePath = dictionaryPackagePath;
        _dictionaryLoadError = dictionaryLoadError;
        _isUsingFallbackDictionary = isUsingFallbackDictionary;
        _ipcServer = new XiaoXiImeIpcServer(
            ProcessKeyAsync,
            GetSnapshotAsync,
            GetUiStateAsync,
            GetHostStatusAsync,
            ResetSessionAsync,
            SetCompositionAsync,
            QueryConversionListAsync,
            RegisterWordAsync,
            options);
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
            _lastActiveSessionId = sessionId;
            return Task.FromResult(new ImeProcessKeyResponse(
                result,
                sessionId,
                request.Generation,
                request.SequenceNumber));
        }
    }

    public Task<ImeProcessResult> ProcessKeyAsync(ImeKey key) =>
        ProcessKeyAsync(key, ImeSessionId.Default);

    public async Task<ImeProcessResult> ProcessKeyAsync(ImeKey key, ImeSessionId sessionId)
    {
        var response = await ProcessKeyAsync(new ImeProcessKeyRequest(key, sessionId)).ConfigureAwait(false);
        return response.Result;
    }

    public Task<ImeSessionSnapshot> GetSnapshotAsync() => GetSnapshotAsync(new ImeSnapshotRequest());

    public Task<ImeSessionSnapshot> GetSnapshotAsync(ImeSessionId sessionId) =>
        GetSnapshotAsync(new ImeSnapshotRequest(sessionId));

    public Task<ImeSessionSnapshot> GetSnapshotAsync(ImeSnapshotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_syncRoot)
        {
            return Task.FromResult(GetOrCreateContext(ResolveSessionId(request.SessionId)).Snapshot);
        }
    }

    public Task<ImeUiState> GetUiStateAsync() => GetUiStateAsync(new ImeUiStateRequest());

    public Task<ImeUiState> GetUiStateAsync(ImeSessionId sessionId) =>
        GetUiStateAsync(new ImeUiStateRequest(sessionId));

    public Task<ImeUiState> GetUiStateAsync(ImeUiStateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_syncRoot)
        {
            return Task.FromResult(ImeUiState.FromSnapshot(
                GetOrCreateContext(ResolveSessionId(request.SessionId)).Snapshot,
                CreateDictionaryDiagnosticText(),
                isHostUnavailable: false,
                isUsingFallbackDictionary: _isUsingFallbackDictionary));
        }
    }

    public Task<ImeResetSessionResponse> ResetSessionAsync(ImeSessionId sessionId) =>
        ResetSessionAsync(new ImeResetSessionRequest(sessionId));

    public Task<ImeResetSessionResponse> ResetAllSessionsAsync() =>
        ResetSessionAsync(new ImeResetSessionRequest(ResetAll: true));

    public Task<ImeResetSessionResponse> ResetSessionAsync(ImeResetSessionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_syncRoot)
        {
            if (request.ResetAll)
            {
                _imeContexts.Clear();
                _lastActiveSessionId = ImeSessionId.Default;
                return Task.FromResult(new ImeResetSessionResponse(ImeSessionId.Unspecified, true));
            }

            var sessionId = ResolveSessionId(request.SessionId);
            var removed = _imeContexts.Remove(sessionId);
            if (removed && _lastActiveSessionId == sessionId)
            {
                _lastActiveSessionId = ImeSessionId.Default;
            }

            return Task.FromResult(new ImeResetSessionResponse(sessionId, removed || !_imeContexts.ContainsKey(sessionId)));
        }
    }

    public Task<ImeProcessResult> SetCompositionAsync(string composition) =>
        SetCompositionAsync(composition, ImeSessionId.Default);

    public async Task<ImeProcessResult> SetCompositionAsync(string composition, ImeSessionId sessionId)
    {
        var response = await SetCompositionAsync(new ImeSetCompositionRequest(composition, sessionId)).ConfigureAwait(false);
        return response.Result;
    }

    public Task<ImeSetCompositionResponse> SetCompositionAsync(ImeSetCompositionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_syncRoot)
        {
            var sessionId = request.EffectiveSessionId;
            var result = GetOrCreateContext(sessionId).SetComposition(request.Composition ?? string.Empty);
            _lastActiveSessionId = sessionId;
            return Task.FromResult(new ImeSetCompositionResponse(result, sessionId));
        }
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

    public Task<ImeConversionListResponse> QueryConversionListAsync(ImeConversionListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_syncRoot)
        {
            var sessionId = ResolveSessionId(request.SessionId);
            var context = GetOrCreateContext(sessionId);
            var source = request.Source ?? string.Empty;
            var candidates = (request.Kind == ImeConversionListKind.ReverseConversion
                    ? context.QueryReverseConversionList(source, request.MaxCount)
                    : context.QueryConversionList(source, request.MaxCount))
                .ToArray();
            return Task.FromResult(new ImeConversionListResponse(candidates, sessionId));
        }
    }

    public Task<ImeRegisterWordResponse> RegisterWordAsync(ImeRegisterWordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_syncRoot)
        {
            var sessionId = ResolveSessionId(request.SessionId);
            var context = GetOrCreateContext(sessionId);
            var succeeded = request.Action switch
            {
                ImeRegisterWordAction.Register => context.RegisterWord(request.Reading ?? string.Empty, request.Text ?? string.Empty),
                ImeRegisterWordAction.Unregister => context.UnregisterWord(request.Reading ?? string.Empty, request.Text ?? string.Empty),
                ImeRegisterWordAction.Enumerate => true,
                _ => false,
            };
            if (request.Action is ImeRegisterWordAction.Register or ImeRegisterWordAction.Unregister
                && succeeded
                && _userDictionary is not null)
            {
                _userDictionarySaveCoordinator?.RequestSave(_userDictionary.GetEntries());
            }

            var entries = request.Action == ImeRegisterWordAction.Enumerate
                ? context.EnumerateRegisterWords(request.Reading, request.Text)
                    .Select(entry => new ImeRegisterWordEntry(entry.Reading, entry.Text))
                    .ToArray()
                : [];
            return Task.FromResult(new ImeRegisterWordResponse(succeeded, entries, sessionId));
        }
    }

    public Task<ImeHostStatus> GetHostStatusAsync()
    {
        lock (_syncRoot)
        {
            return Task.FromResult(new ImeHostStatus(
                _started,
                _lastError,
                _dictionaryPackagePath,
                _isUsingFallbackDictionary,
                _userDictionaryPath,
                _userDictionarySaveError ?? _userDictionaryLoadError,
                _isolatedUserDictionaryPath,
                ImeAboutState.SeWzc,
                _dictionaryLoadError,
                _dictionaryInputScheme));
        }
    }

    private string? CreateDictionaryDiagnosticText()
    {
        var parts = new List<string>();
        if (_isUsingFallbackDictionary)
        {
            parts.Add(string.IsNullOrWhiteSpace(_dictionaryLoadError)
                ? "Using the minimal fallback dictionary."
                : $"Using the minimal fallback dictionary: {_dictionaryLoadError}");
        }

        var userDictionaryError = _userDictionarySaveError ?? _userDictionaryLoadError;
        if (!string.IsNullOrWhiteSpace(userDictionaryError))
        {
            parts.Add($"User dictionary error: {userDictionaryError}");
        }

        return parts.Count == 0 ? null : string.Join(' ', parts);
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            _started = false;
        }

        _userDictionarySaveCoordinator?.Dispose();
        _ipcServer.Dispose();
    }

    private ImeSessionId ResolveSessionId(ImeSessionId sessionId)
    {
        return sessionId.IsUnspecified ? _lastActiveSessionId : sessionId.Effective;
    }

    private ImeContext GetOrCreateContext(ImeSessionId sessionId)
    {
        sessionId = sessionId.Effective;
        if (_imeContexts.TryGetValue(sessionId, out var imeContext))
        {
            return imeContext;
        }

        imeContext = _createImeContext();
        _imeContexts.Add(sessionId, imeContext);
        return imeContext;
    }

    private static RuntimeDictionary LoadRuntimeDictionary(
        string? dictionaryPackagePath,
        string? userDictionaryPath,
        string? inputScheme,
        string? hostBaseDirectory)
    {
        var requestedScheme = DictionaryPackageLocations.ResolveInputScheme(inputScheme);
        var packagePath = DictionaryPackageLocations.ResolvePackageDirectory(
            dictionaryPackagePath,
            requestedScheme,
            hostBaseDirectory);
        var resolvedUserDictionaryPath = Path.GetFullPath(userDictionaryPath ?? UserDictionaryLocations.GetDefaultFilePath());
        var userDictionaryLoad = UserDictionaryStore.Load(resolvedUserDictionaryPath);

        try
        {
            var dictionary = DictionaryPackageLoader.Load(packagePath);
            return new RuntimeDictionary(
                dictionary,
                packagePath,
                dictionary.InputScheme,
                null,
                IsFallback: false,
                resolvedUserDictionaryPath,
                userDictionaryLoad);
        }
        catch (DictionaryPackageException exception)
        {
            return new RuntimeDictionary(
                InMemoryImeDictionary.CreateMinimalFallback(),
                packagePath,
                requestedScheme,
                exception.Message,
                IsFallback: true,
                resolvedUserDictionaryPath,
                userDictionaryLoad);
        }
    }

    private static Func<ImeContext> CreateSingleContextFactory(ImeContext imeContext)
    {
        ArgumentNullException.ThrowIfNull(imeContext);
        var used = false;
        return () =>
        {
            if (used)
            {
                return new ImeContext(InMemoryImeDictionary.CreateMinimalFallback());
            }

            used = true;
            return imeContext;
        };
    }

    private sealed record RuntimeDictionary(
        IImeDictionary Dictionary,
        string PackagePath,
        string? InputScheme,
        string? LoadError,
        bool IsFallback,
        string UserDictionaryPath,
        UserDictionaryLoadResult UserDictionaryLoad);
}
