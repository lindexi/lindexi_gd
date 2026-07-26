using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace XiaoXiIme.ImeModule;

[StructLayout(LayoutKind.Sequential)]
public struct ImeKeystrokeDiagnosticSnapshot
{
    public const uint CurrentVersion = 1;

    public uint Version;
    public uint ImeProcessKeyCallCount;
    public uint ImeToAsciiExCallCount;
    public uint LastProcessVirtualKey;
    public uint LastProcessHandled;
    public uint LastToAsciiVirtualKey;
    public uint LastToAsciiHandled;
    public uint LastCompositionWriteSucceeded;
    public uint LastMessageCount;
    public uint LastReturnValue;
}

public static unsafe class ImeKeystrokeDiagnostics
{
    private static int s_imeProcessKeyCallCount;
    private static int s_imeToAsciiExCallCount;
    private static int s_lastProcessVirtualKey;
    private static int s_lastProcessHandled;
    private static int s_lastToAsciiVirtualKey;
    private static int s_lastToAsciiHandled;
    private static int s_lastCompositionWriteSucceeded;
    private static int s_lastMessageCount;
    private static int s_lastReturnValue;

    [UnmanagedCallersOnly(EntryPoint = "XiaoXiImeResetKeystrokeDiagnostics", CallConvs = [typeof(CallConvStdcall)])]
    public static void ResetExport()
    {
        Reset();
    }

    [UnmanagedCallersOnly(EntryPoint = "XiaoXiImeGetKeystrokeDiagnostics", CallConvs = [typeof(CallConvStdcall)])]
    public static int GetSnapshotExport(ImeKeystrokeDiagnosticSnapshot* snapshot, uint snapshotSize)
    {
        if (snapshot is null || snapshotSize < sizeof(ImeKeystrokeDiagnosticSnapshot))
        {
            return 0;
        }

        *snapshot = GetSnapshot();
        return 1;
    }

    internal static void RecordImeProcessKey(uint virtualKey, bool handled)
    {
        Interlocked.Increment(ref s_imeProcessKeyCallCount);
        Volatile.Write(ref s_lastProcessVirtualKey, unchecked((int)virtualKey));
        Volatile.Write(ref s_lastProcessHandled, handled ? 1 : 0);
    }

    internal static void RecordImeToAsciiEx(uint virtualKey, bool handled, bool compositionWriteSucceeded, uint messageCount, uint returnValue)
    {
        Interlocked.Increment(ref s_imeToAsciiExCallCount);
        Volatile.Write(ref s_lastToAsciiVirtualKey, unchecked((int)virtualKey));
        Volatile.Write(ref s_lastToAsciiHandled, handled ? 1 : 0);
        Volatile.Write(ref s_lastCompositionWriteSucceeded, compositionWriteSucceeded ? 1 : 0);
        Volatile.Write(ref s_lastMessageCount, unchecked((int)messageCount));
        Volatile.Write(ref s_lastReturnValue, unchecked((int)returnValue));
    }

    internal static void Reset()
    {
        Volatile.Write(ref s_imeProcessKeyCallCount, 0);
        Volatile.Write(ref s_imeToAsciiExCallCount, 0);
        Volatile.Write(ref s_lastProcessVirtualKey, 0);
        Volatile.Write(ref s_lastProcessHandled, 0);
        Volatile.Write(ref s_lastToAsciiVirtualKey, 0);
        Volatile.Write(ref s_lastToAsciiHandled, 0);
        Volatile.Write(ref s_lastCompositionWriteSucceeded, 0);
        Volatile.Write(ref s_lastMessageCount, 0);
        Volatile.Write(ref s_lastReturnValue, 0);
    }

    internal static ImeKeystrokeDiagnosticSnapshot GetSnapshot()
    {
        return new ImeKeystrokeDiagnosticSnapshot
        {
            Version = ImeKeystrokeDiagnosticSnapshot.CurrentVersion,
            ImeProcessKeyCallCount = unchecked((uint)Volatile.Read(ref s_imeProcessKeyCallCount)),
            ImeToAsciiExCallCount = unchecked((uint)Volatile.Read(ref s_imeToAsciiExCallCount)),
            LastProcessVirtualKey = unchecked((uint)Volatile.Read(ref s_lastProcessVirtualKey)),
            LastProcessHandled = unchecked((uint)Volatile.Read(ref s_lastProcessHandled)),
            LastToAsciiVirtualKey = unchecked((uint)Volatile.Read(ref s_lastToAsciiVirtualKey)),
            LastToAsciiHandled = unchecked((uint)Volatile.Read(ref s_lastToAsciiHandled)),
            LastCompositionWriteSucceeded = unchecked((uint)Volatile.Read(ref s_lastCompositionWriteSucceeded)),
            LastMessageCount = unchecked((uint)Volatile.Read(ref s_lastMessageCount)),
            LastReturnValue = unchecked((uint)Volatile.Read(ref s_lastReturnValue)),
        };
    }
}
