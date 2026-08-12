namespace XiaoXiIme.ImeInterop;

public static class ImeExportsContract
{
    public const string ModuleFileExtension = ".ime";
    public const int ImeUiClassBufferLength = 16;
    public const string ImeUiClassName = "XiaoXiImeUIWnd";

    public static ImeInquireInfo CreateDefaultInquireInfo()
    {
        return new ImeInquireInfo
        {
            PrivateDataSize = 0,
            Property = ImeConstants.ImePropKbdCharFirst | ImeConstants.ImePropSpecialUi | ImeConstants.ImePropCandidateListStartsAtOne | ImeConstants.ImePropUnicode,
            ConversionCaps = ImeConstants.ImeCmodeNative | ImeConstants.ImeCmodeFullShape,
            SentenceCaps = 0,
            UiCaps = 0,
            SetCompositionStringCaps = ImeConstants.SCSCapsCompStr,
            SelectCaps = 0,
        };
    }
}
