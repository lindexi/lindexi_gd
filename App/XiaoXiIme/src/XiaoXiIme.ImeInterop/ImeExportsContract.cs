using System.Runtime.InteropServices;

namespace XiaoXiIme.ImeInterop;

public static class ImeExportsContract
{
    public const string ModuleFileExtension = ".ime";
    public const string ImeMenuClassName = "XiaoXiImeMenuWindow";
    public const string ImeUiClassName = "XiaoXiImeUiWindow";

    public static ImeInquireInfo CreateDefaultInquireInfo()
    {
        return new ImeInquireInfo
        {
            Size = (uint)Marshal.SizeOf<ImeInquireInfo>(),
            ImeVersion = ImeConstants.ImeVersion0400,
            ImeProperty = ImeConstants.ImePropAtCaret | ImeConstants.ImePropUnicode | ImeConstants.ImePropCompleteOnUnselect,
            ConversionCaps = ImeConstants.ImeCmodeNative | ImeConstants.ImeCmodeFullShape | ImeConstants.ImeCmodeNoConversion,
            SentenceCaps = 0,
            UiCaps = 0,
            SetCompositionStringCaps = ImeConstants.SCSCapsCompStr | ImeConstants.SCSCapsMakeRead,
            SelectCaps = ImeConstants.SelectCapsConversion,
        };
    }
}
