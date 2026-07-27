using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

public static unsafe class ImeExports
{
    private static ImeCompositionContextWriter s_compositionContextWriter = new(ImmContextAccessor.Instance);

    [UnmanagedCallersOnly(EntryPoint = "ImeInquire", CallConvs = [typeof(CallConvStdcall)])]
    public static int ImeInquire(ImeInquireInfo* inquireInfo, char* className, uint systemInfoFlags)
    {
        return ImeInquireManaged(inquireInfo, className, systemInfoFlags);
    }

    internal static int ImeInquireManaged(ImeInquireInfo* inquireInfo, char* className, uint systemInfoFlags)
    {
        try
        {
            if (inquireInfo is null)
            {
                return 0;
            }

            *inquireInfo = ImeExportsContract.CreateDefaultInquireInfo();
            if (className is not null)
            {
                CopyNullTerminated(ImeExportsContract.ImeUiClassName, className, 80);
            }

            return 1;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeConfigure", CallConvs = [typeof(CallConvStdcall)])]
    public static int ImeConfigure(nint keyboardLayout, nint window, uint mode, void* data) => 0;

    [UnmanagedCallersOnly(EntryPoint = "ImeConversionList", CallConvs = [typeof(CallConvStdcall)])]
    public static uint ImeConversionList(nint inputContext, char* source, void* destination, uint bufferLength, uint flag) => 0;

    [UnmanagedCallersOnly(EntryPoint = "ImeDestroy", CallConvs = [typeof(CallConvStdcall)])]
    public static int ImeDestroy(uint reserved) => 1;

    [UnmanagedCallersOnly(EntryPoint = "ImeEscape", CallConvs = [typeof(CallConvStdcall)])]
    public static nint ImeEscape(nint inputContext, uint escape, void* data) => 0;

    [UnmanagedCallersOnly(EntryPoint = "ImeProcessKey", CallConvs = [typeof(CallConvStdcall)])]
    public static int ImeProcessKey(nint inputContext, uint virtualKey, nint keyData, byte* keyState)
    {
        return ImeProcessKeyManaged(inputContext, virtualKey, keyData, keyState) ? 1 : 0;
    }

    internal static bool ImeProcessKeyManaged(nint inputContext, uint virtualKey, nint keyData, byte* keyState)
    {
        try
        {
            var handled = ImeModuleRuntime.ShouldProcessVirtualKey((ushort)virtualKey, unchecked((uint)keyData), new HImc(inputContext));
            ImeKeystrokeDiagnostics.RecordImeProcessKey(virtualKey, handled);
            return handled;
        }
        catch
        {
            ImeKeystrokeDiagnostics.RecordImeProcessKey(virtualKey, false);
            return false;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeSelect", CallConvs = [typeof(CallConvStdcall)])]
    public static int ImeSelect(nint inputContext, int select) => 1;

    [UnmanagedCallersOnly(EntryPoint = "ImeSetActiveContext", CallConvs = [typeof(CallConvStdcall)])]
    public static int ImeSetActiveContext(nint inputContext, int active) => 1;

    [UnmanagedCallersOnly(EntryPoint = "ImeSetCompositionString", CallConvs = [typeof(CallConvStdcall)])]
    public static int ImeSetCompositionString(nint inputContext, uint index, void* composition, uint compositionLength, void* reading, uint readingLength) => 0;

    [UnmanagedCallersOnly(EntryPoint = "ImeToAsciiEx", CallConvs = [typeof(CallConvStdcall)])]
    public static uint ImeToAsciiEx(uint virtualKey, uint scanCode, byte* keyState, nint transKey, uint state, nint inputContext)
    {
        return ImeToAsciiExManaged(virtualKey, scanCode, keyState, transKey, state, inputContext);
    }

    internal static uint ImeToAsciiExManaged(uint virtualKey, uint scanCode, byte* keyState, nint transKey, uint state, nint inputContext)
    {
        try
        {
            var result = ImeModuleRuntime.ConvertVirtualKey((ushort)virtualKey, scanCode);
            var compositionWriteSucceeded = s_compositionContextWriter.TryWrite(new HImc(inputContext), result);
            var messages = ImeTransMsgBuilder.BuildMessages(result);
            var written = ImeTransMsgWriter.Write(transKey, messages);
            var returnValue = written != 0 ? written : result.Handled ? 1u : 0u;
            ImeKeystrokeDiagnostics.RecordImeToAsciiEx(virtualKey, result.Handled, compositionWriteSucceeded, written, returnValue);
            return returnValue;
        }
        catch
        {
            ImeKeystrokeDiagnostics.RecordImeToAsciiEx(virtualKey, false, false, 0, 0);
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "NotifyIME", CallConvs = [typeof(CallConvStdcall)])]
    public static int NotifyIme(nint inputContext, uint action, uint index, uint value)
    {
        return 0;
    }

    public static ImeInquireInfo CreateInquireInfoForTesting()
    {
        return ImeExportsContract.CreateDefaultInquireInfo();
    }

    internal static void SetCompositionContextWriterForTesting(ImeCompositionContextWriter? writer)
    {
        s_compositionContextWriter = writer ?? new ImeCompositionContextWriter(ImmContextAccessor.Instance);
    }

    private static void CopyNullTerminated(string value, char* destination, int destinationLength)
    {
        if (destination is null || destinationLength <= 0)
        {
            return;
        }

        var length = Math.Min(value.Length, destinationLength - 1);
        for (var i = 0; i < length; i++)
        {
            destination[i] = value[i];
        }

        destination[length] = '\0';
    }
}
