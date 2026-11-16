namespace XiaoXiIme.ImeInterop;

public unsafe struct CandidateInfo
{
    public uint Size;
    public uint Count;
    public fixed uint Offset[(int)ImeConstants.CandidateWindowCount];
}