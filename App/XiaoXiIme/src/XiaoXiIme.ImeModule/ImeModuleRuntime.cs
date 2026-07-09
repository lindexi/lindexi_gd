using XiaoXiIme.Foundation;
using XiaoXiIme.ImeInterop;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("XiaoXiIme.ImeModule.Tests")]

namespace XiaoXiIme.ImeModule;

public static class ImeModuleRuntime
{
    private static readonly Lock SyncRoot = new();
    private static ImeHostBridge? s_bridge;
    private static ImeSessionSnapshot s_snapshot = ImeSessionSnapshot.Empty;
    private static ImeCompositionContextReader s_compositionContextReader = new(ImmContextAccessor.Instance);
    private static Func<ImeKey, ImeProcessResult>? s_processKeyForTesting;

    public static ImeProcessResult ProcessVirtualKey(ushort virtualKey, uint modifiers = 0)
    {
        var key = ImeKeyTranslator.Translate(virtualKey, modifiers);
        if (key.Kind is ImeKeyKind.Other)
        {
            return new ImeProcessResult(ImeSessionSnapshot.Empty, CommitText: null, Handled: false);
        }

        var result = ProcessKey(key);
        SetSnapshot(result.Snapshot);
        return result;
    }

    public static ImeToAsciiResult ConvertVirtualKey(ushort virtualKey, uint modifiers = 0)
    {
        return ImeToAsciiResult.FromProcessResult(ProcessVirtualKey(virtualKey, modifiers));
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
            return s_snapshot.IsComposing;
        }
    }

    internal static void SetBridgeForTesting(ImeHostBridge? bridge)
    {
        lock (SyncRoot)
        {
            s_bridge?.Dispose();
            s_bridge = bridge;
            s_snapshot = ImeSessionSnapshot.Empty;
            s_processKeyForTesting = null;
        }
    }

    internal static void SetProcessKeyHandlerForTesting(Func<ImeKey, ImeProcessResult>? processKey)
    {
        lock (SyncRoot)
        {
            s_processKeyForTesting = processKey;
            s_snapshot = ImeSessionSnapshot.Empty;
        }
    }

    internal static ImeSessionSnapshot GetSnapshotForTesting()
    {
        lock (SyncRoot)
        {
            return s_snapshot;
        }
    }

    internal static void SetSnapshotForTesting(ImeSessionSnapshot snapshot)
    {
        SetSnapshot(snapshot);
    }

    internal static void SetCompositionContextReaderForTesting(ImeCompositionContextReader? reader)
    {
        s_compositionContextReader = reader ?? new ImeCompositionContextReader(ImmContextAccessor.Instance);
    }

    private static void SetSnapshot(ImeSessionSnapshot snapshot)
    {
        lock (SyncRoot)
        {
            s_snapshot = snapshot;
        }
    }

    private static ImeHostBridge GetBridge()
    {
        lock (SyncRoot)
        {
            return s_bridge ??= new ImeHostBridge();
        }
    }

    private static ImeProcessResult ProcessKey(ImeKey key)
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

        return (bridge ?? GetBridge()).ProcessKey(key);
    }
}
