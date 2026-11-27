namespace XiaoXiIme.ImeInterop;

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
    public Guid ContextIdentity;
}