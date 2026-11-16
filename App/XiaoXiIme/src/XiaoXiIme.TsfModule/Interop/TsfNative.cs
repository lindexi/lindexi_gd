namespace XiaoXiIme.TsfModule;

internal static class TsfNative
{
    internal static Guid IUnknownId { get; } = new("00000000-0000-0000-C000-000000000046");
    internal static Guid ITextInputProcessorId { get; } = new("AA80E7F7-2021-11D2-93E0-0060B067B86E");
    internal static Guid IKeystrokeManagerId { get; } = new("AA80E7F0-2021-11D2-93E0-0060B067B86E");
    internal static Guid IKeyEventSinkId { get; } = new("AA80E7F5-2021-11D2-93E0-0060B067B86E");
    internal static Guid IEditSessionId { get; } = new("AA80E803-2021-11D2-93E0-0060B067B86E");

    internal const uint TfEditSessionSync = 0x1;
    internal const uint TfEditSessionRead = 0x2;
    internal const uint TfEditSessionReadWrite = 0x6;
    internal const uint TfEditSessionAsync = 0x8;

    internal const int IUnknownQueryInterfaceSlot = 0;
    internal const int IUnknownAddRefSlot = 1;
    internal const int IUnknownReleaseSlot = 2;
    internal const int TextInputProcessorActivateSlot = 3;
    internal const int TextInputProcessorDeactivateSlot = 4;
    internal const int KeystrokeManagerAdviseKeyEventSinkSlot = 3;
    internal const int KeystrokeManagerUnadviseKeyEventSinkSlot = 4;
    internal const int ContextRequestEditSessionSlot = 3;
}