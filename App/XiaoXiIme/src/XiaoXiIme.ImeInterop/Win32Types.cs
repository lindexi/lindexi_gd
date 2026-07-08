using System.Runtime.InteropServices;

namespace XiaoXiIme.ImeInterop;

public static class Imm32Methods
{
    [DllImport("imm32.dll", ExactSpelling = true)]
    public static extern nint ImmLockIMC(HImc inputContext);

    [DllImport("imm32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ImmUnlockIMC(HImc inputContext);

    [DllImport("imm32.dll", ExactSpelling = true)]
    public static extern nint ImmLockIMCC(nint inputContextComponent);

    [DllImport("imm32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ImmUnlockIMCC(nint inputContextComponent);

    [DllImport("imm32.dll", ExactSpelling = true)]
    public static extern nint ImmReSizeIMCC(nint inputContextComponent, uint size);

    [DllImport("imm32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ImmGenerateMessage(HImc inputContext);
}

public struct InputContext
{
    public HWnd Hwnd;
    public nint FOpen;
    public nint FdwConversion;
    public nint FdwSentence;
    public nint HCompStr;
    public nint HCandInfo;
    public nint HGuideLine;
    public nint HPrivate;
    public uint FdwInit;
    public uint FdwUIFlags;
    public uint FdwState;
    public uint FdwChange;
    public uint FdwImeCompatFlags;
    public uint FdwOpenCompatFlags;
    public nint HWndIme;
}

public struct CompositionString
{
    public uint Size;
    public uint CompReadAttrLength;
    public uint CompReadAttrOffset;
    public uint CompReadClauseLength;
    public uint CompReadClauseOffset;
    public uint CompReadStrLength;
    public uint CompReadStrOffset;
    public uint CompAttrLength;
    public uint CompAttrOffset;
    public uint CompClauseLength;
    public uint CompClauseOffset;
    public uint CompStrLength;
    public uint CompStrOffset;
    public uint CursorPos;
    public uint DeltaStart;
    public uint ResultReadClauseLength;
    public uint ResultReadClauseOffset;
    public uint ResultReadStrLength;
    public uint ResultReadStrOffset;
    public uint ResultClauseLength;
    public uint ResultClauseOffset;
    public uint ResultStrLength;
    public uint ResultStrOffset;
    public uint PrivateSize;
    public uint PrivateOffset;
}

public unsafe struct CandidateInfo
{
    public uint Size;
    public uint Count;
    public fixed uint Offset[(int)ImeConstants.CandidateWindowCount];
}

public unsafe struct CandidateList
{
    public uint Size;
    public uint Style;
    public uint Count;
    public uint Selection;
    public uint PageStart;
    public uint PageSize;
    public fixed uint Offset[1];
}

public struct GuideLine
{
    public uint Size;
    public uint Level;
    public uint Index;
    public uint StringLength;
    public uint StringOffset;
    public uint PrivateSize;
    public uint PrivateOffset;
}

public struct ImePrivateData
{
    public uint Size;
    public uint Version;
    public uint CandidateCount;
    public uint CandidateSelection;
    public uint CandidatePageStart;
    public uint CandidatePageSize;
    public uint CompositionLength;
    public uint ReadingLength;
    public uint GuidelineLevel;
    public uint CandidateWindowVisible;
    public uint Reserved0;
    public uint Reserved1;
}

public struct TransMsg
{
    public HWnd Hwnd;
    public uint Message;
    public nuint WParam;
    public nint LParam;
}

public struct TransMsgList
{
    public uint Count;
    public TransMsg Message;
}

public readonly record struct HInstance(nint Value);

public readonly record struct HWnd(nint Value);

public readonly record struct HImc(nint Value);

public readonly record struct HKl(nint Value);
