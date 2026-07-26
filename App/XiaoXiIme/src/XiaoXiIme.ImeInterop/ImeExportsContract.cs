namespace XiaoXiIme.ImeInterop;

public static class ImeExportsContract
{
    public const string ModuleFileExtension = ".ime";
    public const string ImeUiClassName = "XiaoXiImeUiWindow";

    public static ImeInquireInfo CreateDefaultInquireInfo()
    {
        return new ImeInquireInfo
        {
            PrivateDataSize = 0,
            Property = ImeConstants.ImePropAtCaret | ImeConstants.ImePropUnicode | ImeConstants.ImePropCompleteOnUnselect,
            ConversionCaps = ImeConstants.ImeCmodeNative | ImeConstants.ImeCmodeFullShape | ImeConstants.ImeCmodeNoConversion,
            SentenceCaps = 0,
            UiCaps = 0,
            SetCompositionStringCaps = ImeConstants.SCSCapsCompStr | ImeConstants.SCSCapsMakeRead,
            SelectCaps = ImeConstants.SelectCapsConversion,
        };
    }
}
