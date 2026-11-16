namespace XiaoXiIme.ImeInterop;

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