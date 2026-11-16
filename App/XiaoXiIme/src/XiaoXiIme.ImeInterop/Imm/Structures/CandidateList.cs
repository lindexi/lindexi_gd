namespace XiaoXiIme.ImeInterop;

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