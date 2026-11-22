using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeHost;

public sealed class ImeHostService : IDisposable
{
    private readonly ProductSettingsStore? _productSettings;
    private readonly string _inputSchemeSource = "explicitContext";
    private readonly string? _hostBaseDirectory;
    private readonly Func<ImeContext> _createImeContext;
    private readonly string? _dictionaryInputScheme;
    private readonly string? _dictionaryLoadError;
    private readonly string? _dictionaryPackagePath;
    private readonly Dictionary<ImeSessionId, ImeContext> _imeContexts = [];
    private readonly Dictionary<ImeSessionId, long> _keyGenerations = [];
    private readonly Dictionary<ImeSessionId, ImeCandidateAnchor> _anchors = [];
    private readonly Dictionary<string, long> _targetSequences = [];
    private readonly Dictionary<(string Owner, string Context), ImeSessionId> _targetIdentities = [];
    private readonly Dictionary<(uint Process, long Window), ImeSessionId> _activeTargets = [];
    private readonly HashSet<ImeSessionId> _retiredTargets = [];
    private readonly XiaoXiImeIpcServer _ipcServer;
    private readonly System.Threading.Channels.Channel<Func<CancellationToken, Task>> _input =
        System.Threading.Channels.Channel.CreateBounded<Func<CancellationToken, Task>>
            (new System.Threading.Channels.BoundedChannelOptions(128) { SingleReader = true });
    private readonly Dictionary<ImeSessionId, (string Pipe, long Generation)> _replyPipes = []; 
    private readonly CancellationTokenSource _inputShutdown = new();
    private Task? _inputWorker;
    private readonly bool _isUsingFallbackDictionary;
    private readonly string? _isolatedUserDictionaryPath;
    private readonly string? _userDictionaryLoadError;
    private readonly string? _userDictionaryPath;
    private readonly UserDictionary? _userDictionary;
    private readonly UserDictionaryLearningCoordinator? _userDictionarySaveCoordinator;
    private readonly object _syncRoot = new();
    private bool _started;
    private string? _lastError;
    private string? _userDictionarySaveError => _userDictionarySaveCoordinator?.LastError;
    private ImeSessionId _lastActiveSessionId = ImeSessionId.Default;

    /// <param name="hostBaseDirectory">
    /// Optional installed host directory used to resolve the default full Pinyin or Xiaohe package layout.
    /// Explicit <paramref name="dictionaryPackagePath"/> values still take precedence.
    /// </param>
    public ImeHostService
    (
        XiaoXiImeIpcOptions? options = null,
        string? dictionaryPackagePath = null,
        string? userDictionaryPath = null,
        string? inputScheme = null,
        string? hostBaseDirectory = null,
        string? productSettingsPath = null
    )
        : this
        (
            new ProductSettingsStore(productSettingsPath, allowReadOnly: true), options, dictionaryPackagePath, userDictionaryPath,
            inputScheme, hostBaseDirectory
        )
    {
    }

    private ImeHostService
    (
        ProductSettingsStore settings, XiaoXiImeIpcOptions? options, string? packagePath, string? userPath,
        string? inputScheme, string? hostBaseDirectory
    )
        : this
        (
            LoadRuntimeDictionary
            (
                packagePath, userPath,
                !string.IsNullOrWhiteSpace(inputScheme)
                    ? inputScheme
                    : ProductSettingsStore.GetScheme(settings.Settings.InputScheme), hostBaseDirectory
            ), options
        )
    {
        _productSettings = settings;
        var rules = GetRules(settings.Settings.FuzzyRuleMask);
        _userDictionary?.EnableBackgroundPublication(lexicon => ImeContext.PrepareLexicon(lexicon, rules));
        _hostBaseDirectory = hostBaseDirectory;
        _inputSchemeSource = !string.IsNullOrWhiteSpace(packagePath) ? "explicitPath" :
            !string.IsNullOrWhiteSpace(inputScheme) ? "explicitParameter" :
            settings.Settings.Revision > 0 ? "userSettings" : "default";
    }

    /// <summary>Creates a Host around an explicitly supplied input context.</summary>
    public ImeHostService(ImeContext imeContext, XiaoXiImeIpcOptions? options = null)
        : this(CreateSingleContextFactory(imeContext), options)
    {
    }

    internal ImeHostService(Func<ImeContext> createImeContext, XiaoXiImeIpcOptions? options = null)
        : this
        (
            createImeContext, options, dictionaryPackagePath: null, dictionaryLoadError: null,
            isUsingFallbackDictionary: false
        )
    {
    }

    private ImeHostService(RuntimeDictionary runtimeDictionary, XiaoXiImeIpcOptions? options)
    {
        var userDictionary = new UserDictionary
            (runtimeDictionary.Dictionary, runtimeDictionary.UserDictionaryLoad.Entries);
        _userDictionary = userDictionary;
        _dictionaryPackagePath = runtimeDictionary.PackagePath;
        _dictionaryInputScheme = runtimeDictionary.InputScheme;
        _dictionaryLoadError = runtimeDictionary.LoadError;
        _isUsingFallbackDictionary = runtimeDictionary.IsFallback;
        _userDictionaryPath = runtimeDictionary.UserDictionaryPath;
        _userDictionaryLoadError = runtimeDictionary.UserDictionaryLoad.Error;
        _isolatedUserDictionaryPath = runtimeDictionary.UserDictionaryLoad.IsolatedCorruptFilePath;
        var publicationRules = GetRules(_productSettings?.Settings.FuzzyRuleMask ?? 0);
        userDictionary.EnableBackgroundPublication(lexicon => ImeContext.PrepareLexicon(lexicon, publicationRules));
        _userDictionarySaveCoordinator = new UserDictionaryLearningCoordinator
        (
            userDictionary,
            runtimeDictionary.UserDictionaryLoad is { Error: not null, IsolatedCorruptFilePath: null }
                ? static (_, _) => Task.CompletedTask
                : (entries, token) => UserDictionaryStore.SaveAsync(runtimeDictionary.UserDictionaryPath, entries, token)
        );
        var initialContext = new ImeContext
        (
            userDictionary, learning =>
            {
                foreach (var segment in learning.Segments)
                {
                    _userDictionarySaveCoordinator.TryEnqueue(segment.Text, segment.CanonicalReading);
                }
            }
        );
        _createImeContext = () => initialContext.PrepareDictionaryChange(userDictionary)
            .PrepareFuzzyRulesChange(GetRules(_productSettings?.Settings.FuzzyRuleMask ?? 0));
        _ipcServer = new XiaoXiImeIpcServer
        (
            ProcessKeyAsync,
            GetSnapshotAsync,
            GetUiStateAsync,
            GetHostStatusAsync,
            ResetSessionAsync,
            SetCompositionAsync,
            QueryConversionListAsync,
            RegisterWordAsync,
            options,
            GetProductSettingsAsync,
            SaveProductSettingsAsync,
            UpdateInputTargetAsync
        );
    }

    private ImeHostService
    (
        Func<ImeContext> createImeContext,
        XiaoXiImeIpcOptions? options,
        string? dictionaryPackagePath,
        string? dictionaryLoadError,
        bool isUsingFallbackDictionary
    )
    {
        _createImeContext = createImeContext ?? throw new ArgumentNullException(nameof(createImeContext));
        _dictionaryPackagePath = dictionaryPackagePath;
        _dictionaryLoadError = dictionaryLoadError;
        _isUsingFallbackDictionary = isUsingFallbackDictionary;
        _ipcServer = new XiaoXiImeIpcServer
        (
            ProcessKeyAsync,
            GetSnapshotAsync,
            GetUiStateAsync,
            GetHostStatusAsync,
            ResetSessionAsync,
            SetCompositionAsync,
            QueryConversionListAsync,
            RegisterWordAsync,
            options,
            updateInputTargetAsync: UpdateInputTargetAsync
        );
    }

    /// <summary>Reads saved preferences without reading or resetting input sessions.</summary>
    public Task<ImeProductSettingsResponse> GetProductSettingsAsync() => Task.FromResult
    (
        _productSettings?.Read(_dictionaryInputScheme, _inputSchemeSource)
        ?? new ImeProductSettingsResponse
            (Guid.Empty, new(), _dictionaryInputScheme, _inputSchemeSource, false, ImeProductSettingsError.Unsupported)
    );

    /// <summary>Atomically persists preferences; input schemes apply only on the next Host startup.</summary>
    public Task<ImeProductSettingsResponse> SaveProductSettingsAsync(ImeSaveProductSettingsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _productSettings?.SaveAsync
               (
                   request, _dictionaryInputScheme, _inputSchemeSource, _hostBaseDirectory, (next, commit) =>
                   {
                       lock (_syncRoot)
                       {
                           var rules = GetRules(next.FuzzyRuleMask);
                           if (next.FuzzyRuleMask != _productSettings.Settings.FuzzyRuleMask)
                           {
                               _userDictionary?.EnableBackgroundPublication
                                   (lexicon => ImeContext.PrepareLexicon(lexicon, rules));
                           }

                           var prepared = next.FuzzyRuleMask == _productSettings.Settings.FuzzyRuleMask
                               ? null
                               : _imeContexts.ToDictionary(p => p.Key, p => p.Value.PrepareFuzzyRulesChange(rules));
                           commit();
                           if (prepared is not null)
                           {
                               foreach (var pair in prepared)
                               {
                                   _imeContexts[pair.Key] = pair.Value;
                               }
                           }
                       }
                   }
               )
               ?? GetProductSettingsAsync();
    }

    private static ImeFuzzyRule[] GetRules
        (int mask) => Enum.GetValues<ImeFuzzyRule>().Where(r => (mask & (1 << (int)r)) != 0).ToArray();

    /// <summary>Signals that candidate state should be read again by an in-process observer.</summary>
    public event Action? CandidateStateChanged;

    public void Start()
    {
        try
        {
            if (_inputWorker is null)
            {
                _ipcServer.RegisterInputEvents(input => _input.Writer.TryWrite(token => ProcessInputEventAsync(input, token)));
                _inputWorker = Task.Run(() => DeliverInputAsync(_inputShutdown.Token));
            }
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

    private async Task DeliverInputAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var apply in _input.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    await apply(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Trace.TraceError("[ImeHost][InputWorker] {0}", exception);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Service shutdown ends the input stream.
        }
    }

    private async Task ProcessInputEventAsync(ImeInputEvent input, CancellationToken token)
    {
        var request = input.Request;
        lock (_syncRoot)
        {
            if (_retiredTargets.Contains(request.SessionId) || request.Generation < _keyGenerations.GetValueOrDefault(request.SessionId))
            {
                return;
            }
            _replyPipes[request.SessionId] = (input.ReplyPipe, request.Generation);
        }

        if (request.Key.Kind == ImeKeyKind.Space &&
            Environment.GetEnvironmentVariable("XIAOXIIME_LIFECYCLE_LOG") is { Length: > 0 } diagnosticPath)
        {
            var delayRequest = diagnosticPath + ".delay-request";
            if (File.Exists(delayRequest))
            {
                File.Move(delayRequest, diagnosticPath + ".delay-started", overwrite: true);
                await Task.Delay(TimeSpan.FromMilliseconds(1500), token).ConfigureAwait(false);
            }
        }

        ImeProcessKeyResponse result;
        try
        {
            result = await ProcessKeyAsync(request).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("[ImeHost][ProcessInput] SessionId={0}; Sequence={1}; Error={2}",
                request.SessionId, request.SequenceNumber, exception);
            ClearFailedComposition(request.SessionId, request.Generation);
            result = new(new(ImeSessionSnapshot.Empty, null, true), request.SessionId, request.Generation, request.SequenceNumber);
        }
        await SendResultAsync(input.ReplyPipe, result, token).ConfigureAwait(false);
    }

    /// <summary>Queues selection from the exact candidate page displayed by the product UI.</summary>
    public void SelectDisplayedCandidate(ImeSessionId id, long revision, int index)
    {
        _input.Writer.TryWrite(token => SelectDisplayedCandidateAsync(id, revision, index, token));
    }

    private async Task SelectDisplayedCandidateAsync(ImeSessionId id, long revision, int index, CancellationToken token)
    {
        (string Pipe, long Generation) destination;
        ImeProcessResult result;
        lock (_syncRoot)
        {
            if (!_replyPipes.TryGetValue(id, out destination) ||
                !_imeContexts.TryGetValue(id, out var context) || !_activeTargets.ContainsValue(id))
            {
                return;
            }
            result = context.SelectDisplayedCandidate(revision, index);
            if (!result.Handled)
            {
                return;
            }
            CandidateStateChanged?.Invoke();
        }
        await SendResultAsync(destination.Pipe, new(result, id, destination.Generation), token).ConfigureAwait(false);
    }

    private async Task SendResultAsync(string pipe, ImeProcessKeyResponse result, CancellationToken token)
    {
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(XiaoXiImeIpcOptions.DefaultRequestTimeout);
            await _ipcServer.ApplyInputResultAsync(pipe, result, deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or OperationCanceledException
            or dotnetCampus.Ipc.Exceptions.IpcPeerConnectionBrokenException)
        {
            ClearFailedComposition(result.SessionId, result.Generation);
            System.Diagnostics.Trace.TraceError($"[ImeHost][SendResult] SessionId={result.SessionId}; Error={exception}");
        }
    }

    private void ClearFailedComposition(ImeSessionId sessionId, long generation)
    {
        lock (_syncRoot)
        {
            if (_keyGenerations.GetValueOrDefault(sessionId) == generation && _imeContexts.Remove(sessionId))
            {
                CandidateStateChanged?.Invoke();
            }
        }
    }

    /// <summary>Applies an ordered IMM observation without processing a synthetic key or resetting composition.</summary>
    public Task<ImeInputTargetUpdateResponse> UpdateInputTargetAsync(ImeInputTargetUpdate request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_syncRoot)
        {
            if (string.IsNullOrWhiteSpace(request.OwnerId) || string.IsNullOrWhiteSpace(request.ContextKey)
                                                           || request.SessionId.IsUnspecified ||
                                                           request.SequenceNumber <= 0
                                                           || _retiredTargets.Contains(request.SessionId)
                                                           || (_targetSequences.TryGetValue
                                                                   (request.OwnerId, out var previous) &&
                                                               request.SequenceNumber <= previous))
            {
                return Task.FromResult(new ImeInputTargetUpdateResponse(false));
            }

            _targetSequences[request.OwnerId] = request.SequenceNumber;
            var key = (request.OwnerId, request.ContextKey);
            if (_targetIdentities.TryGetValue(key, out var old) && old != request.SessionId)
            {
                _retiredTargets.Add(old);
                _keyGenerations.Remove(old);
                _replyPipes.Remove(old);
                _imeContexts.Remove(old);
                _anchors.Remove(old);
                foreach (var target in _activeTargets.Where(pair => pair.Value == old).Select
                             (pair => pair.Key).ToArray())
                {
                    _activeTargets.Remove(target);
                }
            }

            _targetIdentities[key] = request.SessionId;
            // A live HIMC can move to another HWND without ending its composition lifetime.
            foreach (var target in _activeTargets.Where(pair => pair.Value == request.SessionId).Select
                         (pair => pair.Key).ToArray())
            {
                _activeTargets.Remove(target);
            }

            if (request.Anchor is { } anchor && anchor.SessionId == request.SessionId)
            {
                _anchors[request.SessionId] = anchor;
                var target = (anchor.ProcessId, anchor.WindowHandle);
                if (request.Active)
                {
                    _activeTargets[target] = request.SessionId;
                    _lastActiveSessionId = request.SessionId;
                }
                else if (_activeTargets.GetValueOrDefault(target) == request.SessionId)
                {
                    _activeTargets.Remove(target);
                }
            }

            CandidateStateChanged?.Invoke();
            return Task.FromResult(new ImeInputTargetUpdateResponse(true));
        }
    }

    public Task<ImeProcessKeyResponse> ProcessKeyAsync(ImeProcessKeyRequest request)
    {
        lock (_syncRoot)
        {
            var sessionId = request.EffectiveSessionId;
            var generation = _keyGenerations.GetValueOrDefault(sessionId);
            if (_retiredTargets.Contains(sessionId) || request.Generation < generation)
            {
                return Task.FromResult
                (
                    new ImeProcessKeyResponse
                    (
                        new(ImeSessionSnapshot.Empty, null, false),
                        sessionId, request.Generation, request.SequenceNumber
                    )
                );
            }

            if (request.Generation > generation)
            {
                _keyGenerations[sessionId] = request.Generation;
                if (request.ResumeGeneration == 0 || request.ResumeGeneration != generation)
                {
                    _imeContexts.Remove(sessionId);
                }
            }

            if (!_imeContexts.TryGetValue(sessionId, out var imeContext))
            {
                imeContext = _createImeContext();
                _imeContexts.Add(sessionId, imeContext);
            }

            if (request.Anchor is { } anchor && anchor.SessionId == sessionId)
            {
                if (!_anchors.TryGetValue(sessionId, out var cached) ||
                    anchor.ObservationSequence >= cached.ObservationSequence)
                {
                    _anchors[sessionId] = anchor;
                }
            }
            else if (!_targetIdentities.ContainsValue(sessionId))
            {
                _anchors.Remove(sessionId);
            }

            ImeProcessResult result;
            try
            {
                var before = imeContext.Snapshot.Composition.Reading;
                var started = System.Diagnostics.Stopwatch.GetTimestamp();
                result = imeContext.ProcessKey(request.Key);
                if (Environment.GetEnvironmentVariable("XIAOXIIME_LIFECYCLE_LOG") is not null)
                {
                    Console.WriteLine
                    (
                        $"G02 key session={sessionId} key={request.Key} before={before} handled={result.Handled} commit={result.CommitText} after={result.Snapshot.Composition.Reading} elapsedMs={System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds:F3}"
                    );
                }
            }
            catch (Exception error)
            {
                if (Environment.GetEnvironmentVariable("XIAOXIIME_LIFECYCLE_LOG") is not null)
                {
                    Console.Error.WriteLine($"G02 key session={sessionId} key={request.Key} error={error}");
                }

                throw;
            }

            if (!_targetIdentities.ContainsValue(sessionId))
            {
                _lastActiveSessionId = sessionId;
            }

            CandidateStateChanged?.Invoke();
            return Task.FromResult
            (
                new ImeProcessKeyResponse
                (
                    result,
                    sessionId,
                    request.Generation,
                    request.SequenceNumber
                )
            );
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
            var sessionId = ResolveSessionId(request.SessionId);
            if (request.SessionId.IsUnspecified && request.FocusWindowHandle is { } focusWindow)
            {
                var matches = _anchors.Where
                (pair => pair.Value.WindowHandle == focusWindow
                         && pair.Value.ProcessId == request.FocusProcessId
                ).Select(pair => pair.Key).ToArray();
                if (request.FocusProcessId is { } pid && _activeTargets.TryGetValue((pid, focusWindow), out var active))
                {
                    sessionId = active;
                }
                else if (matches.Length == 1 && !_targetIdentities.ContainsValue(matches[0]))
                {
                    sessionId = matches[0];
                }
                else
                {
                    return Task.FromResult(ImeUiState.FromSnapshot(ImeSessionSnapshot.Empty));
                }
            }

            _anchors.TryGetValue(sessionId, out var anchor);
            return Task.FromResult
            (
                ImeUiState.FromSnapshot
                    (
                        GetOrCreateContext(sessionId).Snapshot,
                        CreateDictionaryDiagnosticText(),
                        isHostUnavailable: false,
                        isUsingFallbackDictionary: _isUsingFallbackDictionary
                    ) with
                    {
                        SessionId = sessionId,
                        CandidateRevision = GetOrCreateContext(sessionId).CandidateRevision,
                        Anchor = anchor,
                        AnchorX = anchor?.ScreenX ?? 0,
                        AnchorY = anchor?.ScreenY ?? 0,
                    }
            );
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
                _replyPipes.Clear();
                _anchors.Clear();
                _activeTargets.Clear();
                _lastActiveSessionId = ImeSessionId.Default;
                CandidateStateChanged?.Invoke();
                return Task.FromResult(new ImeResetSessionResponse(ImeSessionId.Unspecified, true));
            }

            var sessionId = ResolveSessionId(request.SessionId);
            _replyPipes.Remove(sessionId);
            _anchors.Remove(sessionId);
            foreach (var target in _activeTargets.Where(pair => pair.Value == sessionId).Select
                         (pair => pair.Key).ToArray())
            {
                _activeTargets.Remove(target);
            }

            var removed = _imeContexts.Remove(sessionId);
            if (removed && _lastActiveSessionId == sessionId)
            {
                _lastActiveSessionId = ImeSessionId.Default;
            }

            CandidateStateChanged?.Invoke();
            return Task.FromResult
                (new ImeResetSessionResponse(sessionId, removed || !_imeContexts.ContainsKey(sessionId)));
        }
    }

    public Task<ImeProcessResult> SetCompositionAsync(string composition) =>
        SetCompositionAsync(composition, ImeSessionId.Default);

    public async Task<ImeProcessResult> SetCompositionAsync(string composition, ImeSessionId sessionId)
    {
        var response = await SetCompositionAsync(new ImeSetCompositionRequest(composition, sessionId)).ConfigureAwait
            (false);
        return response.Result;
    }

    public Task<ImeSetCompositionResponse> SetCompositionAsync(ImeSetCompositionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_syncRoot)
        {
            var sessionId = request.EffectiveSessionId;
            if (_retiredTargets.Contains(sessionId))
            {
                return Task.FromResult
                    (new ImeSetCompositionResponse(new(ImeSessionSnapshot.Empty, null, false), sessionId));
            }

            var result = GetOrCreateContext(sessionId).SetComposition(request.Composition ?? string.Empty);
            _lastActiveSessionId = sessionId;
            CandidateStateChanged?.Invoke();
            return Task.FromResult(new ImeSetCompositionResponse(result, sessionId));
        }
    }

    public Task<ImeCandidate[]> QueryConversionListAsync(string source) =>
        QueryConversionListAsync(source, ImeSessionId.Default);

    public Task<ImeCandidate[]> QueryConversionListAsync(string source, ImeSessionId sessionId) =>
        QueryConversionListAsync(source, sessionId, ImeConversionListKind.Conversion);

    public async Task<ImeCandidate[]> QueryConversionListAsync
    (
        string source,
        ImeSessionId sessionId,
        ImeConversionListKind kind
    )
    {
        var response = await QueryConversionListAsync
            (new ImeConversionListRequest(source, SessionId: sessionId, Kind: kind)).ConfigureAwait(false);
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
                ImeRegisterWordAction.Register => context.RegisterWord
                    (request.Reading ?? string.Empty, request.Text ?? string.Empty),
                ImeRegisterWordAction.Unregister => context.UnregisterWord
                    (request.Reading ?? string.Empty, request.Text ?? string.Empty),
                ImeRegisterWordAction.Enumerate => true,
                _ => false,
            };
            if (request.Action is ImeRegisterWordAction.Register or ImeRegisterWordAction.Unregister
                && succeeded
                && _userDictionary is not null)
            {
                _userDictionarySaveCoordinator?.RequestSave();
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
            return Task.FromResult
            (
                new ImeHostStatus
                (
                    _started,
                    _lastError,
                    _dictionaryPackagePath,
                    _isUsingFallbackDictionary,
                    _userDictionaryPath,
                    _userDictionarySaveError ?? _userDictionaryLoadError,
                    _isolatedUserDictionaryPath,
                    ImeAboutState.SeWzc,
                    _dictionaryLoadError,
                    _dictionaryInputScheme
                )
            );
        }
    }

    private string? CreateDictionaryDiagnosticText()
    {
        var parts = new List<string>();
        if (_isUsingFallbackDictionary)
        {
            parts.Add
            (
                string.IsNullOrWhiteSpace(_dictionaryLoadError)
                    ? "Using the minimal fallback dictionary."
                    : $"Using the minimal fallback dictionary: {_dictionaryLoadError}"
            );
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

        _input.Writer.TryComplete();
        _input.Writer.TryComplete();
        _inputShutdown.Cancel();
        _inputWorker?.GetAwaiter().GetResult();
        _inputShutdown.Dispose();
        _userDictionarySaveCoordinator?.Dispose();
        _ipcServer.Dispose();
        _productSettings?.Dispose();
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

        var diagnostic = Environment.GetEnvironmentVariable("XIAOXIIME_VERIFY_ANCHOR") == "1";
        var started = System.Diagnostics.Stopwatch.StartNew();
        if (diagnostic)
        {
            Console.WriteLine($"G02 context-create begin session={sessionId} pid={Environment.ProcessId}");
        }

        imeContext = _createImeContext();
        if (diagnostic)
        {
            Console.WriteLine
                ($"G02 context-create end session={sessionId} elapsedMs={started.Elapsed.TotalMilliseconds:F1}");
        }

        _imeContexts.Add(sessionId, imeContext);
        return imeContext;
    }

    private static RuntimeDictionary LoadRuntimeDictionary
    (
        string? dictionaryPackagePath,
        string? userDictionaryPath,
        string? inputScheme,
        string? hostBaseDirectory
    )
    {
        var requestedScheme = DictionaryPackageLocations.ResolveInputScheme(inputScheme);
        var packagePath = DictionaryPackageLocations.ResolvePackageDirectory
        (
            dictionaryPackagePath,
            requestedScheme,
            hostBaseDirectory
        );
        var resolvedUserDictionaryPath = Path.GetFullPath
            (userDictionaryPath ?? UserDictionaryLocations.GetDefaultFilePath());
        var userDictionaryLoad = UserDictionaryStore.Load(resolvedUserDictionaryPath);

        try
        {
            var dictionary = DictionaryPackageLoader.Load(packagePath);
            return new RuntimeDictionary
            (
                dictionary,
                packagePath,
                dictionary.InputScheme,
                null,
                IsFallback: false,
                resolvedUserDictionaryPath,
                userDictionaryLoad
            );
        }
        catch (DictionaryPackageException exception)
        {
            return new RuntimeDictionary
            (
                InMemoryImeDictionary.CreateMinimalFallback(),
                packagePath,
                requestedScheme,
                exception.Message,
                IsFallback: true,
                resolvedUserDictionaryPath,
                userDictionaryLoad
            );
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

    private sealed record RuntimeDictionary
    (
        IImeDictionary Dictionary,
        string PackagePath,
        string? InputScheme,
        string? LoadError,
        bool IsFallback,
        string UserDictionaryPath,
        UserDictionaryLoadResult UserDictionaryLoad
    );
}