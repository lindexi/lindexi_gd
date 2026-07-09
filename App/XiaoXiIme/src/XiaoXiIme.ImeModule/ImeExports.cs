using System.Runtime.InteropServices;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

public static unsafe class ImeExports
{
    private static ImeCompositionContextWriter s_compositionContextWriter = new(ImmContextAccessor.Instance);

    [UnmanagedCallersOnly(EntryPoint = "ImeInquire")]
    public static uint ImeInquire(ImeInquireInfo* inquireInfo, char* className, uint systemInfoFlags)
    {
        return ImeInquireManaged(inquireInfo, className, systemInfoFlags);
    }

    internal static uint ImeInquireManaged(ImeInquireInfo* inquireInfo, char* className, uint systemInfoFlags)
    {
        try
        {
            if (inquireInfo is null)
            {
                return 0;
            }

            *inquireInfo = ImeExportsContract.CreateDefaultInquireInfo();
            CopyNullTerminated(ImeExportsContract.ImeMenuClassName, inquireInfo->ImeMenuClassName, 80);
            if (className is not null)
            {
                CopyNullTerminated(ImeExportsContract.ImeUiClassName, className, 80);
            }

            return inquireInfo->Size;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeProcessKey")]
    public static uint ImeProcessKey(nint inputContext, uint virtualKey, uint keyData, byte* keyState)
    {
        return ImeProcessKeyManaged(inputContext, virtualKey, keyData, keyState);
    }

    internal static uint ImeProcessKeyManaged(nint inputContext, uint virtualKey, uint keyData, byte* keyState)
    {
        try
        {
            return ImeModuleRuntime.ShouldProcessVirtualKey((ushort)virtualKey, keyData, new HImc(inputContext)) ? 1u : 0u;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeToAsciiEx")]
    public static uint ImeToAsciiEx(uint virtualKey, uint scanCode, byte* keyState, nint transKey, uint state, nint inputContext)
    {
        return ImeToAsciiExManaged(virtualKey, scanCode, keyState, transKey, state, inputContext);
    }

    internal static uint ImeToAsciiExManaged(uint virtualKey, uint scanCode, byte* keyState, nint transKey, uint state, nint inputContext)
    {
        try
        {
            var result = ImeModuleRuntime.ConvertVirtualKey((ushort)virtualKey, scanCode);
            s_compositionContextWriter.TryWrite(new HImc(inputContext), result);
            var messages = ImeTransMsgBuilder.BuildMessages(result);
            var written = ImeTransMsgWriter.Write(transKey, messages);
            return written != 0 ? written : result.Handled ? 1u : 0u;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeSelect")]
    public static uint ImeSelect(nint inputContext, uint select)
    {
        return 1;
    }

    [UnmanagedCallersOnly(EntryPoint = "NotifyIME")]
    public static uint NotifyIme(nint inputContext, uint action, uint index, uint value)
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
