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

public struct Point
{
    public int X;
    public int Y;
}

public struct Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

public unsafe struct LogFont
{
    public int Height;
    public int Width;
    public int Escapement;
    public int Orientation;
    public int Weight;
    public byte Italic;
    public byte Underline;
    public byte StrikeOut;
    public byte CharSet;
    public byte OutPrecision;
    public byte ClipPrecision;
    public byte Quality;
    public byte PitchAndFamily;
    public fixed char FaceName[32];
}

public struct CompositionForm
{
    public uint Style;
    public Point CurrentPosition;
    public Rect Area;
}

public struct CandidateForm
{
    public uint Index;
    public uint Style;
    public Point CurrentPosition;
    public Rect Area;
}

public unsafe struct InputContext
{
    public HWnd Hwnd;
    public int FOpen;
    public Point StatusWindowPosition;
    public Point SoftKeyboardPosition;
    public uint FdwConversion;
    public uint FdwSentence;
    public LogFont Font;
    public CompositionForm CompositionForm;
    public fixed byte CandidateForms[4 * 32];
    public nint HCompStr;
    public nint HCandInfo;
    public nint HGuideLine;
    public nint HPrivate;
    public uint MessageBufferCount;
    public nint HMessageBuffer;
    public uint FdwInit;
    public fixed uint Reserved[3];
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
