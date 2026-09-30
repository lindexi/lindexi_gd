using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeInterop;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeModule;

public static unsafe class ImeExports
{
    private static ImeCompositionContextWriter s_compositionContextWriter = new(ImmContextAccessor.Instance);
    private static RegisterWordEnumProc? s_registerWordEnumProcForTesting;

    internal unsafe delegate int RegisterWordEnumProc(char* reading, uint style, char* value, void* data);

    internal static void SetRegisterWordEnumProcForTesting(RegisterWordEnumProc? callback)
    {
        s_registerWordEnumProcForTesting = callback;
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeInquire", CallConvs = [typeof(CallConvStdcall)])]
    public static int ImeInquire(ImeInquireInfo* inquireInfo, char* className, uint systemInfoFlags)
    {
        ImeKeystrokeDiagnostics.RecordImeInquire();
        return ImeUiWindowClass.EnsureRegistered()
            ? ImeInquireManaged(inquireInfo, className, systemInfoFlags)
            : 0;
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
                CopyNullTerminated(ImeExportsContract.ImeUiClassName, className, ImeExportsContract.ImeUiClassBufferLength);
            }

            return 1;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeConfigure", CallConvs = [typeof(CallConvStdcall)])]
    public static int ImeConfigure(nint keyboardLayout, nint window, uint mode, void* data)
    {
        return ImeConfigureManaged(keyboardLayout, window, mode, data);
    }

    internal static int ImeConfigureManaged(nint keyboardLayout, nint window, uint mode, void* data)
    {
        try
        {
            if (mode != ImeConstants.ImeConfigRegisterWord || data is null)
            {
                return 0;
            }

            var registerWord = (RegisterWord*)data;
            return MutateRegisterWord(
                ImeRegisterWordAction.Register,
                registerWord->Reading,
                ImeConstants.ImeRegWordStyleUserFirst,
                registerWord->Word) ? 1 : 0;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeConversionList", CallConvs = [typeof(CallConvStdcall)])]
    public static uint ImeConversionList(nint inputContext, char* source, void* destination, uint bufferLength, uint flag)
    {
        return ImeConversionListManaged(inputContext, source, destination, bufferLength, flag);
    }

    internal static uint ImeConversionListManaged(nint inputContext, char* source, void* destination, uint bufferLength, uint flag)
    {
        try
        {
            if (flag is not (ImeConstants.GclConversion or ImeConstants.GclReverseConversion or ImeConstants.GclReverseLength))
            {
                return 0;
            }

            var sourceText = ReadNullTerminated(source);
            if (string.IsNullOrEmpty(sourceText))
            {
                return 0;
            }

            var reverse = flag is ImeConstants.GclReverseConversion or ImeConstants.GclReverseLength;
            var candidates = reverse
                ? ImeModuleRuntime.QueryReverseConversionList(sourceText, new HImc(inputContext))
                : ImeModuleRuntime.QueryConversionList(sourceText, new HImc(inputContext));
            if (candidates.Length == 0)
            {
                return 0;
            }

            var requiredSize = ImeCompositionContextWriter.GetRequiredConversionListSize(candidates, reverse);
            if (flag == ImeConstants.GclReverseLength || destination is null || bufferLength < requiredSize)
            {
                return requiredSize;
            }

            return s_compositionContextWriter.TryWriteConversionList(destination, bufferLength, candidates, reverse)
                ? requiredSize
                : 0;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeDestroy", CallConvs = [typeof(CallConvStdcall)])]
    public static int ImeDestroy(uint reserved) => ImeDestroyManaged(reserved);

    internal static int ImeDestroyManaged(uint reserved)
    {
        try
        {
            ImeModuleRuntime.ResetAllSessions();
            return 1;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeEscape", CallConvs = [typeof(CallConvStdcall)])]
    public static nint ImeEscape(nint inputContext, uint escape, void* data)
    {
        return ImeEscapeManaged(inputContext, escape, data);
    }

    internal static nint ImeEscapeManaged(nint inputContext, uint escape, void* data)
    {
        try
        {
            return escape switch
            {
                ImeConstants.ImeEscQuerySupport => QueryEscapeSupport(data),
                ImeConstants.ImeEscImeName => WriteImeName(data),
                _ => 0,
            };
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeProcessKey", CallConvs = [typeof(CallConvStdcall)])]
    public static int ImeProcessKey(nint inputContext, uint virtualKey, nint keyData, byte* keyState)
    {
        return ImeProcessKeyManaged(inputContext, virtualKey, keyData, keyState) ? 1 : 0;
    }

    internal static bool ImeProcessKeyManaged(nint inputContext, uint virtualKey, nint keyData, byte* keyState)
    {
        var normalizedVirtualKey = (ushort)virtualKey;
        const uint keyTransitionState = 0x80000000;
        if ((unchecked((uint)keyData) & keyTransitionState) != 0)
        {
            ImeKeystrokeDiagnostics.RecordImeProcessKey(normalizedVirtualKey, false);
            return false;
        }

        try
        {
            var handled = ImeModuleRuntime.ShouldProcessVirtualKey(
                normalizedVirtualKey,
                GetModifiers(keyState),
                new HImc(inputContext));
            ImeKeystrokeDiagnostics.RecordImeProcessKey(normalizedVirtualKey, handled);
            return handled;
        }
        catch (Exception exception)
        {
            ImeKeystrokeDiagnostics.RecordImeProcessKey(normalizedVirtualKey, false, unchecked((uint)exception.HResult));
            return false;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeRegisterWord", CallConvs = [typeof(CallConvStdcall)])]
    public static int ImeRegisterWord(char* reading, uint style, char* value)
    {
        return ImeRegisterWordManaged(reading, style, value);
    }

    internal static int ImeRegisterWordManaged(char* reading, uint style, char* value)
    {
        return MutateRegisterWord(ImeRegisterWordAction.Register, reading, style, value) ? 1 : 0;
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeUnregisterWord", CallConvs = [typeof(CallConvStdcall)])]
    public static int ImeUnregisterWord(char* reading, uint style, char* value)
    {
        return ImeUnregisterWordManaged(reading, style, value);
    }

    internal static int ImeUnregisterWordManaged(char* reading, uint style, char* value)
    {
        return MutateRegisterWord(ImeRegisterWordAction.Unregister, reading, style, value) ? 1 : 0;
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeGetRegisterWordStyle", CallConvs = [typeof(CallConvStdcall)])]
    public static uint ImeGetRegisterWordStyle(uint itemCount, void* styleBuffer)
    {
        return ImeGetRegisterWordStyleManaged(itemCount, styleBuffer);
    }

    internal static uint ImeGetRegisterWordStyleManaged(uint itemCount, void* styleBuffer)
    {
        try
        {
            if (itemCount == 0 || styleBuffer is null)
            {
                return 1;
            }

            var style = (StyleBuf*)styleBuffer;
            style->Style = ImeConstants.ImeRegWordStyleUserFirst;
            CopyNullTerminated(ImeExportsContract.UserWordStyleDescription, style->Description, ImeConstants.StyleDescriptionBufferLength);
            return 1;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeEnumRegisterWord", CallConvs = [typeof(CallConvStdcall)])]
    public static uint ImeEnumRegisterWord(nint enumProc, char* reading, uint style, char* value, void* data)
    {
        return ImeEnumRegisterWordManaged(enumProc, reading, style, value, data);
    }

    internal static uint ImeEnumRegisterWordManaged(nint enumProc, char* reading, uint style, char* value, void* data)
    {
        try
        {
            var managedCallback = s_registerWordEnumProcForTesting;
            if (enumProc == 0 && managedCallback is null)
            {
                return 0;
            }

            if (style != 0 && style != ImeConstants.ImeRegWordStyleUserFirst)
            {
                return 0;
            }

            var response = ImeModuleRuntime.RegisterWord(new ImeRegisterWordRequest(
                ImeRegisterWordAction.Enumerate,
                ReadNullTerminated(reading) ?? string.Empty,
                ReadNullTerminated(value) ?? string.Empty));
            var nativeCallback = enumProc == 0
                ? null
                : (delegate* unmanaged[Stdcall]<char*, uint, char*, void*, int>)enumProc;
            uint count = 0;
            foreach (var entry in response.Entries)
            {
                fixed (char* readingPointer = entry.Reading)
                fixed (char* textPointer = entry.Text)
                {
                    var continued = managedCallback is not null
                        ? managedCallback(readingPointer, ImeConstants.ImeRegWordStyleUserFirst, textPointer, data)
                        : nativeCallback(readingPointer, ImeConstants.ImeRegWordStyleUserFirst, textPointer, data);
                    if (continued == 0)
                    {
                        break;
                    }
                }

                count++;
            }

            return count;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeSelect", CallConvs = [typeof(CallConvStdcall)])]
    public static int ImeSelect(nint inputContext, int select)
    {
        return ImeSelectManaged(inputContext, select);
    }

    internal static int ImeSelectManaged(nint inputContext, int select)
    {
        ImeKeystrokeDiagnostics.RecordImeSelect();
        try
        {
            if (select == 0)
            {
                ApplyControlKey(ImeConstants.VkEscape, inputContext);
                ImeModuleRuntime.ResetSession(new HImc(inputContext));
            }

            return 1;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeSetActiveContext", CallConvs = [typeof(CallConvStdcall)])]
    public static int ImeSetActiveContext(nint inputContext, int active)
    {
        return ImeSetActiveContextManaged(inputContext, active);
    }

    internal static int ImeSetActiveContextManaged(nint inputContext, int active)
    {
        ImeKeystrokeDiagnostics.RecordImeSetActiveContext();
        try
        {
            if (active == 0)
            {
                ApplyControlKey(ImeConstants.VkEscape, inputContext);
                ImeModuleRuntime.ResetSession(new HImc(inputContext));
            }

            return 1;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeSetCompositionString", CallConvs = [typeof(CallConvStdcall)])]
    public static int ImeSetCompositionString(nint inputContext, uint index, void* composition, uint compositionLength, void* reading, uint readingLength)
    {
        return ImeSetCompositionStringManaged(inputContext, index, composition, compositionLength, reading, readingLength);
    }

    internal static int ImeSetCompositionStringManaged(nint inputContext, uint index, void* composition, uint compositionLength, void* reading, uint readingLength)
    {
        try
        {
            if (index != ImeConstants.ScsSetStr)
            {
                return 0;
            }

            var compositionText = ReadWideString(composition, compositionLength);
            if (compositionText is null)
            {
                compositionText = ReadWideString(reading, readingLength);
            }

            if (compositionText is null)
            {
                return 0;
            }

            var himc = new HImc(inputContext);
            var result = ImeModuleRuntime.ConvertComposition(compositionText, himc);
            if (!result.Handled)
            {
                return 0;
            }

            s_compositionContextWriter.TryWrite(himc, result);
            return 1;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ImeToAsciiEx", CallConvs = [typeof(CallConvStdcall)])]
    public static uint ImeToAsciiEx(uint virtualKey, uint scanCode, byte* keyState, nint transKey, uint state, nint inputContext)
    {
        return ImeToAsciiExManaged(virtualKey, scanCode, keyState, transKey, state, inputContext);
    }

    internal static uint ImeToAsciiExManaged(uint virtualKey, uint scanCode, byte* keyState, nint transKey, uint state, nint inputContext)
    {
        var normalizedVirtualKey = (ushort)virtualKey;
        uint stage = 1;
        try
        {
            var key = ImeKeyTranslator.Translate(normalizedVirtualKey, GetModifiers(keyState));
            stage = 2;
            var processResult = key.Kind is ImeKeyKind.Other
                ? new ImeProcessResult(ImeSessionSnapshot.Empty, CommitText: null, Handled: false)
                : ImeModuleRuntime.ProcessTranslatedKey(key, new HImc(inputContext));
            var result = ImeToAsciiResult.FromProcessResult(processResult);
            stage = 3;
            var compositionWriteSucceeded = s_compositionContextWriter.TryWrite(new HImc(inputContext), result);
            stage = 4;
            var messages = ImeTransMsgBuilder.BuildMessages(result);
            stage = 5;
            var written = ImeTransMsgWriter.Write(transKey, messages);
            var returnValue = written != 0 ? written : result.Handled ? 1u : 0u;
            ImeKeystrokeDiagnostics.RecordImeToAsciiEx(normalizedVirtualKey, result.Handled, compositionWriteSucceeded, written, returnValue, stage: stage);
            return returnValue;
        }
        catch (Exception exception)
        {
            ImeKeystrokeDiagnostics.RecordImeToAsciiEx(normalizedVirtualKey, false, false, 0, 0, unchecked((uint)exception.HResult), stage);
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "NotifyIME", CallConvs = [typeof(CallConvStdcall)])]
    public static int NotifyIme(nint inputContext, uint action, uint index, uint value)
    {
        return NotifyImeManaged(inputContext, action, index, value);
    }

    internal static int NotifyImeManaged(nint inputContext, uint action, uint index, uint value)
    {
        ImeKeystrokeDiagnostics.RecordNotifyIme();
        try
        {
            return action switch
            {
                ImeConstants.NiSelectCandidateStr => ApplyTranslatedKey(
                    ImeKey.SelectCandidate(unchecked((int)value)),
                    inputContext).Handled ? 1 : 0,
                ImeConstants.NiCompositionStr => ApplyCompositionNotify(value, inputContext),
                _ => 0,
            };
        }
        catch
        {
            return 0;
        }
    }

    public static ImeInquireInfo CreateInquireInfoForTesting()
    {
        return ImeExportsContract.CreateDefaultInquireInfo();
    }

    internal static void SetCompositionContextWriterForTesting(ImeCompositionContextWriter? writer)
    {
        s_compositionContextWriter = writer ?? new ImeCompositionContextWriter(ImmContextAccessor.Instance);
    }

    private static int ApplyCompositionNotify(uint value, nint inputContext)
    {
        var virtualKey = value switch
        {
            ImeConstants.CpsCancel => ImeConstants.VkEscape,
            ImeConstants.CpsComplete => ImeConstants.VkSpace,
            ImeConstants.CpsRevert => ImeConstants.VkReturn,
            _ => (ushort)0,
        };
        return virtualKey == 0 ? 0 : ApplyControlKey(virtualKey, inputContext).Handled ? 1 : 0;
    }

    private static ImeToAsciiResult ApplyControlKey(ushort virtualKey, nint inputContext)
    {
        return ApplyTranslatedKey(ImeKeyTranslator.Translate(virtualKey), inputContext);
    }

    private static ImeToAsciiResult ApplyTranslatedKey(ImeKey key, nint inputContext)
    {
        var himc = new HImc(inputContext);
        var result = ImeToAsciiResult.FromProcessResult(ImeModuleRuntime.ProcessTranslatedKey(key, himc));
        s_compositionContextWriter.TryWrite(himc, result);
        return result;
    }

    private static nint QueryEscapeSupport(void* data)
    {
        if (data is null)
        {
            return 0;
        }

        var requestedEscape = *(uint*)data;
        return requestedEscape is ImeConstants.ImeEscQuerySupport or ImeConstants.ImeEscImeName
            ? 1
            : 0;
    }

    private static nint WriteImeName(void* data)
    {
        if (data is null)
        {
            return 0;
        }

        CopyNullTerminated(ImeExportsContract.ImeDisplayName, (char*)data, ImeConstants.ImeNameBufferLength);
        return 1;
    }

    private static bool MutateRegisterWord(ImeRegisterWordAction action, char* reading, uint style, char* value)
    {
        try
        {
            if (style != 0 && style != ImeConstants.ImeRegWordStyleUserFirst)
            {
                return false;
            }

            var readingText = ReadNullTerminated(reading);
            var valueText = ReadNullTerminated(value);
            if (string.IsNullOrEmpty(readingText) || string.IsNullOrEmpty(valueText))
            {
                return false;
            }

            return ImeModuleRuntime.RegisterWord(new ImeRegisterWordRequest(action, readingText, valueText)).Succeeded;
        }
        catch
        {
            return false;
        }
    }

    private static uint GetModifiers(byte* keyState)
    {
        const int shiftVirtualKey = 0x10;
        const byte keyDownMask = 0x80;
        return keyState is not null && (keyState[shiftVirtualKey] & keyDownMask) != 0
            ? ImeKeyTranslator.ShiftModifier
            : 0;
    }

    private static string? ReadNullTerminated(char* value)
    {
        if (value is null)
        {
            return null;
        }

        var length = 0;
        while (value[length] != '\0')
        {
            length++;
            if (length > 1024)
            {
                break;
            }
        }

        return length == 0 ? string.Empty : new string(value, 0, length);
    }

    private static string? ReadWideString(void* value, uint byteLength)
    {
        if (value is null)
        {
            return byteLength == 0 ? string.Empty : null;
        }

        var charCount = (int)(byteLength / sizeof(char));
        if (charCount <= 0)
        {
            return string.Empty;
        }

        var span = new ReadOnlySpan<char>((char*)value, charCount);
        var terminator = span.IndexOf('\0');
        if (terminator >= 0)
        {
            span = span[..terminator];
        }

        return span.Length == 0 ? string.Empty : new string(span);
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
