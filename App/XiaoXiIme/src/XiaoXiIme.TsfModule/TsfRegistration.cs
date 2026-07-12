namespace XiaoXiIme.TsfModule;

public static class TsfRegistration
{
    public static Guid ClassId { get; } = new("8B9C319B-132F-4931-9248-DFC740920F52");

    public static Guid ProfileId { get; } = new("56AA5C4E-AE34-4C76-9B37-22B277B4700D");

    public static Guid DisplayAttributeProviderCategory { get; } = new("046B8C80-1647-40F7-9B21-B93B81AABC1B");

    public const ushort SimplifiedChineseLanguageId = 0x0804;

    public const string DisplayName = "XiaoXi IME (TSF)";
}
