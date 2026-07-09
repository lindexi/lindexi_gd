namespace XiaoXiIme.ImeInterop;

public static class ImeConstants
{
    public const uint ImeVersion0400 = 0x00040000;

    public const uint ImePropAtCaret = 0x00010000;
    public const uint ImePropUnicode = 0x00080000;
    public const uint ImePropCompleteOnUnselect = 0x00100000;

    public const uint ImeCmodeNative = 0x00000001;
    public const uint ImeCmodeFullShape = 0x00000008;
    public const uint ImeCmodeNoConversion = 0x00000100;

    public const uint ImeSmodeNone = 0x00000000;

    public const uint UiCap2700 = 0x00000001;
    public const uint UiCapRot90 = 0x00000002;
    public const uint UiCapRotAny = 0x00000004;

    public const uint SCSCapsCompStr = 0x00000001;
    public const uint SCSCapsMakeRead = 0x00000002;
    public const uint SCSCapsSetReconVert = 0x00000004;

    public const uint SelectCapsConversion = 0x00000001;
    public const uint SelectCapsSentence = 0x00000002;

    public const uint GcsCompReadStr = 0x00000001;
    public const uint GcsCompStr = 0x00000008;
    public const uint GcsCompAttr = 0x00000010;
    public const uint GcsCompClause = 0x00000020;
    public const uint GcsCursorPos = 0x00000080;
    public const uint GcsResultReadStr = 0x00000200;
    public const uint GcsResultStr = 0x00000800;
    public const uint GcsResultClause = 0x00001000;
    public const uint GcsCandidateInfo = 0x00002000;
    public const uint GcsGuideLine = 0x00004000;
    public const uint GcsPrivate = 0x00008000;

    public const uint AttrInput = 0x00;

    public const uint ClauseStart = 0;

    public const uint CandidateListStyleReading = 0x00000010;
    public const uint CandidateWindowCount = 32;
    public const uint CandidatePageSize = 9;

    public const uint GuidelineLevelNone = 0x00000000;
    public const uint GuidelineLevelInfo = 0x00000001;
    public const uint GuidelineLevelWarning = 0x00000002;
    public const uint GuidelineLevelError = 0x00000003;
    public const uint GuidelineLevelReading = 0x00010001;
    public const uint GuidelineLevelNoCandidate = 0x00010002;
    public const uint GuidelineLevelInvalidInput = 0x00010003;
    public const uint GuidelineIndexNone = 0x00000000;
    public const uint XiaoXiImePrivateDataVersion = 2;

    public const uint WmImeComposition = 0x010F;
    public const uint WmImeEndComposition = 0x010E;
    public const uint WmImeStartComposition = 0x010D;
    public const uint WmImeNotify = 0x0282;

    public const uint ImnChangeCandidate = 0x0003;
    public const uint ImnCloseCandidate = 0x0004;
    public const uint ImnOpenCandidate = 0x0005;

    public const ushort VkBack = 0x08;
    public const ushort VkTab = 0x09;
    public const ushort VkReturn = 0x0D;
    public const ushort VkEscape = 0x1B;
    public const ushort VkSpace = 0x20;
    public const ushort VkPrior = 0x21;
    public const ushort VkNext = 0x22;
    public const ushort VkEnd = 0x23;
    public const ushort VkHome = 0x24;
    public const ushort VkLeft = 0x25;
    public const ushort VkUp = 0x26;
    public const ushort VkRight = 0x27;
    public const ushort VkDown = 0x28;
    public const ushort Vk0 = 0x30;
    public const ushort Vk1 = 0x31;
    public const ushort Vk9 = 0x39;
    public const ushort VkA = 0x41;
    public const ushort VkZ = 0x5A;
}
