namespace XiaoXiIme.ImeInterop;

public unsafe struct ImeInquireInfo
{
    public uint Size;
    public uint ImeVersion;
    public uint ImeProperty;
    public uint ConversionCaps;
    public uint SentenceCaps;
    public uint UiCaps;
    public uint SetCompositionStringCaps;
    public uint SelectCaps;
    public fixed char ImeMenuClassName[80];
}
