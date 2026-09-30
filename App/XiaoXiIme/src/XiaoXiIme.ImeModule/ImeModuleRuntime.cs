using XiaoXiIme.Foundation;
using XiaoXiIme.ImeInterop;
using XiaoXiIme.ImeIpc;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("XiaoXiIme.ImeModule.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("XiaoXiIme.SandboxRunner")]

namespace XiaoXiIme.ImeModule;

public static class ImeModuleRuntime
{
    private static readonly Lock SyncRoot = new();
    private static readonly Dictionary<ImeSessionId, ImeSessionSnapshot> s_snapshots = [];
    private static ImeHostBridge? s_bridge;
    private static ImeSessionId s_lastSessionId = ImeSessionId.Default;
    private static ImeProcessResult s_lastResult = new(ImeSessionSnapshot.Empty, null, false);
    private static ImeCompositionContextReader s_compositionContextReader = new(ImmContextAccessor.Instance);
    private static Func<ImeKey, ImeProcessResult>? s_processKeyForTesting;
    private static Func<string, ImeProcessResult>? s_setCompositionForTesting;
    private static Func<string, ImeCandidate[]>? s_queryConversionListForTesting;
    private static Func<string, ImeCandidate[]>? s_queryReverseConversionListForTesting;
    private static Func<ImeRegisterWordRequest, ImeRegisterWordResponse>? s_registerWordForTesting;

    public static ImeProcessResult ProcessVirtualKey(ushort virtualKey, uint modifiers = 0) =>
        ProcessVirtualKey(virtualKey, modifiers, default);

    public static ImeProcessResult ProcessVirtualKey(ushort virtualKey, uint modifiers, HImc inputContext)
    {
        var key = ImeKeyTranslator.Translate(virtualKey, modifiers);
        if (key.Kind is ImeKeyKind.Other)
        {
            return new ImeProcessResult(ImeSessionSnapshot.Empty, CommitText: null, Handled: false);
        }

        return ProcessTranslatedKey(key, inputContext);
    }

    public static ImeProcessResult ProcessTranslatedKey(ImeKey key, HImc inputContext)
    {
        var sessionId = ImeSessionId.FromHimc(inputContext.Value);
        var result = ProcessKey(key, sessionId);
        SetSnapshot(sessionId, result.Snapshot);
        SetLastResult(sessionId, result);
        return result;
    }

    public static ImeToAsciiResult ConvertVirtualKey(ushort virtualKey, uint modifiers = 0) =>
        ConvertVirtualKey(virtualKey, modifiers, default);

    public static ImeToAsciiResult ConvertVirtualKey(ushort virtualKey, uint modifiers, HImc inputContext)
    {
        return ImeToAsciiResult.FromProcessResult(ProcessVirtualKey(virtualKey, modifiers, inputContext));
    }

    public static ImeProcessResult SetComposition(string composition) =>
        SetComposition(composition, default);

    public static ImeProcessResult SetComposition(string composition, HImc inputContext)
    {
        ArgumentNullException.ThrowIfNull(composition);
        var sessionId = ImeSessionId.FromHimc(inputContext.Value);
        var result = SetHostComposition(composition, sessionId);
        SetSnapshot(sessionId, result.Snapshot);
        SetLastResult(sessionId, result);
        return result;
    }

    public static ImeToAsciiResult ConvertComposition(string composition, HImc inputContext)
    {
        return ImeToAsciiResult.FromProcessResult(SetComposition(composition, inputContext));
    }

    public static ImeCandidate[] QueryConversionList(string source) =>
        QueryConversionList(source, default);

    public static ImeCandidate[] QueryConversionList(string source, HImc inputContext)
    {
        ArgumentNullException.ThrowIfNull(source);
        var sessionId = ImeSessionId.FromHimc(inputContext.Value);
        return QueryHostConversionList(source, sessionId, ImeConversionListKind.Conversion);
    }

    public static ImeCandidate[] QueryReverseConversionList(string source) =>
        QueryReverseConversionList(source, default);

    public static ImeCandidate[] QueryReverseConversionList(string source, HImc inputContext)
    {
        ArgumentNullException.ThrowIfNull(source);
        var sessionId = ImeSessionId.FromHimc(inputContext.Value);
        return QueryHostConversionList(source, sessionId, ImeConversionListKind.ReverseConversion);
    }

    public static ImeRegisterWordResponse RegisterWord(ImeRegisterWordRequest request) =>
        RegisterWord(request, default);

    public static ImeRegisterWordResponse RegisterWord(ImeRegisterWordRequest request, HImc inputContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sessionId = ImeSessionId.FromHimc(inputContext.Value);
        return RegisterHostWord(request with { SessionId = sessionId.IsUnspecified ? request.SessionId : sessionId });
    }

    public static ImeResetSessionResponse ResetSession(HImc inputContext)
    {
        var sessionId = ImeSessionId.FromHimc(inputContext.Value);
        var response = ResetHostSession(new ImeResetSessionRequest(sessionId));
        lock (SyncRoot)
        {
            s_snapshots.Remove(sessionId);
            if (s_lastSessionId == sessionId)
            {
                s_lastSessionId = ImeSessionId.Default;
                s_lastResult = new ImeProcessResult(ImeSessionSnapshot.Empty, null, false);
            }
        }

        return response;
    }

    public static ImeResetSessionResponse ResetAllSessions()
    {
        var response = ResetHostSession(new ImeResetSessionRequest(ResetAll: true));
        lock (SyncRoot)
        {
            s_snapshots.Clear();
            s_lastSessionId = ImeSessionId.Default;
            s_lastResult = new ImeProcessResult(ImeSessionSnapshot.Empty, null, false);
        }

        return response;
    }

    public static bool ShouldProcessVirtualKey(ushort virtualKey, uint modifiers = 0)
    {
        return ShouldProcessVirtualKey(virtualKey, modifiers, default);
    }

    public static bool ShouldProcessVirtualKey(ushort virtualKey, uint modifiers, HImc inputContext)
    {
        if (ImeKeyTranslator.ShouldProcess(virtualKey, modifiers))
        {
            return true;
        }

        if (s_compositionContextReader.IsComposing(inputContext))
        {
            return true;
        }

        lock (SyncRoot)
        {
            return GetSnapshot(ImeSessionId.FromHimc(inputContext.Value)).IsComposing;
        }
    }

    internal static void SetBridgeForTesting(ImeHostBridge? bridge)
    {
        lock (SyncRoot)
        {
            s_bridge?.Dispose();
            s_bridge = bridge;
            s_snapshots.Clear();
            s_lastSessionId = ImeSessionId.Default;
            s_lastResult = new ImeProcessResult(ImeSessionSnapshot.Empty, null, false);
            s_processKeyForTesting = null;
            s_setCompositionForTesting = null;
            s_queryConversionListForTesting = null;
            s_queryReverseConversionListForTesting = null;
            s_registerWordForTesting = null;
        }
    }

    internal static void SetProcessKeyHandlerForTesting(Func<ImeKey, ImeProcessResult>? processKey)
    {
        lock (SyncRoot)
        {
            s_processKeyForTesting = processKey;
            s_setCompositionForTesting = null;
            s_snapshots.Clear();
            s_lastSessionId = ImeSessionId.Default;
            s_lastResult = new ImeProcessResult(ImeSessionSnapshot.Empty, null, false);
        }
    }

    internal static void SetCompositionHandlerForTesting(Func<string, ImeProcessResult>? setComposition)
    {
        lock (SyncRoot)
        {
            s_setCompositionForTesting = setComposition;
            s_snapshots.Clear();
            s_lastSessionId = ImeSessionId.Default;
            s_lastResult = new ImeProcessResult(ImeSessionSnapshot.Empty, null, false);
        }
    }

    internal static void SetConversionListHandlerForTesting(Func<string, ImeCandidate[]>? queryConversionList)
    {
        lock (SyncRoot)
        {
            s_queryConversionListForTesting = queryConversionList;
        }
    }

    internal static void SetReverseConversionListHandlerForTesting(Func<string, ImeCandidate[]>? queryReverseConversionList)
    {
        lock (SyncRoot)
        {
            s_queryReverseConversionListForTesting = queryReverseConversionList;
        }
    }

    internal static void SetRegisterWordHandlerForTesting(Func<ImeRegisterWordRequest, ImeRegisterWordResponse>? registerWord)
    {
        lock (SyncRoot)
        {
            s_registerWordForTesting = registerWord;
        }
    }

    internal static ImeSessionSnapshot GetSnapshotForTesting() => GetSnapshotForTesting(s_lastSessionId);

    internal static ImeSessionSnapshot GetSnapshotForTesting(ImeSessionId sessionId)
    {
        lock (SyncRoot)
        {
            return GetSnapshot(sessionId);
        }
    }

    internal static ImeProcessResult GetLastProcessResultForTesting()
    {
        lock (SyncRoot)
        {
            return s_lastResult;
        }
    }

    internal static ImeSessionId GetLastSessionIdForTesting()
    {
        lock (SyncRoot)
        {
            return s_lastSessionId;
        }
    }

    internal static void SetSnapshotForTesting(ImeSessionSnapshot snapshot) =>
        SetSnapshotForTesting(ImeSessionId.Default, snapshot);

    internal static void SetSnapshotForTesting(ImeSessionId sessionId, ImeSessionSnapshot snapshot)
    {
        SetSnapshot(sessionId.Effective, snapshot);
    }

    internal static void SetCompositionContextReaderForTesting(ImeCompositionContextReader? reader)
    {
        s_compositionContextReader = reader ?? new ImeCompositionContextReader(ImmContextAccessor.Instance);
    }

    private static void SetSnapshot(ImeSessionId sessionId, ImeSessionSnapshot snapshot)
    {
        lock (SyncRoot)
        {
            s_snapshots[sessionId.Effective] = snapshot;
        }
    }

    private static ImeSessionSnapshot GetSnapshot(ImeSessionId sessionId)
    {
        return s_snapshots.TryGetValue(sessionId.Effective, out var snapshot)
            ? snapshot
            : ImeSessionSnapshot.Empty;
    }

    private static void SetLastResult(ImeSessionId sessionId, ImeProcessResult result)
    {
        lock (SyncRoot)
        {
            s_lastSessionId = sessionId.Effective;
            s_lastResult = result;
        }
    }

    private static ImeHostBridge GetBridge()
    {
        lock (SyncRoot)
        {
            return s_bridge ??= new ImeHostBridge();
        }
    }

    private static ImeProcessResult ProcessKey(ImeKey key, ImeSessionId sessionId)
    {
        Func<ImeKey, ImeProcessResult>? processKey;
        ImeHostBridge? bridge;
        lock (SyncRoot)
        {
            processKey = s_processKeyForTesting;
            bridge = s_bridge;
        }

        if (processKey is not null)
        {
            return processKey(key);
        }

        return (bridge ?? GetBridge()).ProcessKey(key, sessionId);
    }

    private static ImeProcessResult SetHostComposition(string composition, ImeSessionId sessionId)
    {
        Func<string, ImeProcessResult>? setComposition;
        Func<ImeKey, ImeProcessResult>? processKey;
        ImeHostBridge? bridge;
        lock (SyncRoot)
        {
            setComposition = s_setCompositionForTesting;
            processKey = s_processKeyForTesting;
            bridge = s_bridge;
        }

        if (setComposition is not null)
        {
            return setComposition(composition);
        }

        if (processKey is not null)
        {
            return new ImeProcessResult(ImeSessionSnapshot.Empty, null, false);
        }

        return (bridge ?? GetBridge()).SetComposition(composition, sessionId);
    }

    private static ImeCandidate[] QueryHostConversionList(string source, ImeSessionId sessionId, ImeConversionListKind kind)
    {
        Func<string, ImeCandidate[]>? queryConversionList;
        ImeHostBridge? bridge;
        lock (SyncRoot)
        {
            queryConversionList = kind == ImeConversionListKind.ReverseConversion
                ? s_queryReverseConversionListForTesting
                : s_queryConversionListForTesting;
            bridge = s_bridge;
        }

        if (queryConversionList is not null)
        {
            return queryConversionList(source) ?? [];
        }

        return (bridge ?? GetBridge()).QueryConversionList(source, sessionId, kind) ?? [];
    }

    private static ImeRegisterWordResponse RegisterHostWord(ImeRegisterWordRequest request)
    {
        Func<ImeRegisterWordRequest, ImeRegisterWordResponse>? registerWord;
        ImeHostBridge? bridge;
        lock (SyncRoot)
        {
            registerWord = s_registerWordForTesting;
            bridge = s_bridge;
        }

        if (registerWord is not null)
        {
            return registerWord(request);
        }

        return (bridge ?? GetBridge()).RegisterWord(request);
    }

    private static ImeResetSessionResponse ResetHostSession(ImeResetSessionRequest request)
    {
        Func<ImeKey, ImeProcessResult>? processKey;
        ImeHostBridge? bridge;
        lock (SyncRoot)
        {
            processKey = s_processKeyForTesting;
            bridge = s_bridge;
        }

        if (processKey is not null)
        {
            return new ImeResetSessionResponse(
                request.ResetAll ? ImeSessionId.Unspecified : request.SessionId.Effective,
                true);
        }

        return (bridge ?? GetBridge()).ResetSession(request);
    }
}
