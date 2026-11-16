using System.Runtime.InteropServices;

namespace XiaoXiIme.ImeModule;

[StructLayout(LayoutKind.Sequential)]
public struct ImeKeystrokeDiagnosticSnapshot
{
    public const uint CurrentVersion = 4;

    public uint Version;
    public uint ImeInquireCallCount;
    public uint ImeSelectCallCount;
    public uint ImeSetActiveContextCallCount;
    public uint NotifyImeCallCount;
    public uint ImeProcessKeyCallCount;
    public uint ImeToAsciiExCallCount;
    public uint LastProcessVirtualKey;
    public uint LastProcessHandled;
    public uint LastToAsciiVirtualKey;
    public uint LastToAsciiHandled;
    public uint LastCompositionWriteSucceeded;
    public uint LastMessageCount;
    public uint LastReturnValue;
    public uint ProcessKeyHandledCount;
    public uint ToAsciiHandledCount;
    public uint LastProcessError;
    public uint LastToAsciiError;
    public uint LastToAsciiStage;
}