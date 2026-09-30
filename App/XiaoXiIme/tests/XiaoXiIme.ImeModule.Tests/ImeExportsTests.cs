using XiaoXiIme.ImeInterop;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeIpc;
using System.Runtime.InteropServices;

namespace XiaoXiIme.ImeModule.Tests;

public class ImeExportsTests
{
    [Fact]
    public void ImeInquireInfo_MatchesNativeImeInfoLayout()
    {
        Assert.Equal(7 * sizeof(uint), Marshal.SizeOf<ImeInquireInfo>());
    }

    [Fact]
    public void InputContext_MatchesNativeLayout()
    {
        Assert.Equal(IntPtr.Size == 8 ? 352 : 320, Marshal.SizeOf<InputContext>());
        Assert.Equal(IntPtr.Size == 8 ? 288 : 280, Marshal.OffsetOf<InputContext>(nameof(InputContext.HCompStr)).ToInt32());
        Assert.Equal(IntPtr.Size == 8 ? 328 : 300, Marshal.OffsetOf<InputContext>(nameof(InputContext.HMessageBuffer)).ToInt32());
        Assert.Equal(IntPtr.Size == 8 ? 336 : 304, Marshal.OffsetOf<InputContext>(nameof(InputContext.FdwInit)).ToInt32());
    }

    [Fact]
    public void TransMsgTypes_MatchNativeLayout()
    {
        Assert.Equal(IntPtr.Size == 8 ? 24 : 12, Marshal.SizeOf<TransMsg>());
        Assert.Equal(0, Marshal.OffsetOf<TransMsg>(nameof(TransMsg.Message)).ToInt32());
        Assert.Equal(IntPtr.Size == 8 ? 8 : 4, Marshal.OffsetOf<TransMsg>(nameof(TransMsg.WParam)).ToInt32());
        Assert.Equal(IntPtr.Size == 8 ? 16 : 8, Marshal.OffsetOf<TransMsg>(nameof(TransMsg.LParam)).ToInt32());
        Assert.Equal(IntPtr.Size == 8 ? 8 : 4, Marshal.OffsetOf<TransMsgList>(nameof(TransMsgList.Message)).ToInt32());
    }

    [Fact]
    public void RegisterWord_MatchesNativeLayout()
    {
        Assert.Equal(IntPtr.Size == 8 ? 16 : 8, Marshal.SizeOf<RegisterWord>());
        Assert.Equal(0, Marshal.OffsetOf<RegisterWord>(nameof(RegisterWord.Reading)).ToInt32());
        Assert.Equal(IntPtr.Size == 8 ? 8 : 4, Marshal.OffsetOf<RegisterWord>(nameof(RegisterWord.Word)).ToInt32());
    }

    [Fact]
    public void StyleBuf_MatchesNativeLayout()
    {
        Assert.Equal(4 + (ImeConstants.StyleDescriptionBufferLength * sizeof(char)), Marshal.SizeOf<StyleBuf>());
        Assert.Equal(0, Marshal.OffsetOf<StyleBuf>(nameof(StyleBuf.Style)).ToInt32());
        Assert.Equal(4, Marshal.OffsetOf<StyleBuf>(nameof(StyleBuf.Description)).ToInt32());
    }

    [Fact]
    public void KeystrokeDiagnosticSnapshot_MatchesExportContract()
    {
        Assert.Equal(76, Marshal.SizeOf<ImeKeystrokeDiagnosticSnapshot>());
        Assert.Equal(0, Marshal.OffsetOf<ImeKeystrokeDiagnosticSnapshot>(nameof(ImeKeystrokeDiagnosticSnapshot.Version)).ToInt32());
        Assert.Equal(52, Marshal.OffsetOf<ImeKeystrokeDiagnosticSnapshot>(nameof(ImeKeystrokeDiagnosticSnapshot.LastReturnValue)).ToInt32());
        Assert.Equal(68, Marshal.OffsetOf<ImeKeystrokeDiagnosticSnapshot>(nameof(ImeKeystrokeDiagnosticSnapshot.LastToAsciiError)).ToInt32());
        Assert.Equal(72, Marshal.OffsetOf<ImeKeystrokeDiagnosticSnapshot>(nameof(ImeKeystrokeDiagnosticSnapshot.LastToAsciiStage)).ToInt32());
    }

    [Fact]
    public void KeystrokeDiagnostics_ResetAndRecord_ReturnsLatestTrace()
    {
        const uint vkX = 0x58;
        ImeKeystrokeDiagnostics.Reset();

        ImeKeystrokeDiagnostics.RecordImeProcessKey(ImeConstants.VkA, true);
        ImeKeystrokeDiagnostics.RecordImeProcessKey(vkX, true);
        ImeKeystrokeDiagnostics.RecordImeToAsciiEx(ImeConstants.VkA, true, false, 2, 2);
        ImeKeystrokeDiagnostics.RecordImeToAsciiEx(vkX, true, true, 2, 2);

        var snapshot = ImeKeystrokeDiagnostics.GetSnapshot();

        Assert.Equal(ImeKeystrokeDiagnosticSnapshot.CurrentVersion, snapshot.Version);
        Assert.Equal(2u, snapshot.ImeProcessKeyCallCount);
        Assert.Equal(2u, snapshot.ImeToAsciiExCallCount);
        Assert.Equal(vkX, snapshot.LastProcessVirtualKey);
        Assert.Equal(1u, snapshot.LastProcessHandled);
        Assert.Equal(vkX, snapshot.LastToAsciiVirtualKey);
        Assert.Equal(1u, snapshot.LastToAsciiHandled);
        Assert.Equal(1u, snapshot.LastCompositionWriteSucceeded);
        Assert.Equal(2u, snapshot.LastMessageCount);
        Assert.Equal(2u, snapshot.LastReturnValue);

        ImeKeystrokeDiagnostics.Reset();
        snapshot = ImeKeystrokeDiagnostics.GetSnapshot();
        Assert.Equal(0u, snapshot.ImeProcessKeyCallCount);
        Assert.Equal(0u, snapshot.ImeToAsciiExCallCount);
    }

    [Fact]
    public void CreateInquireInfoForTesting_ReturnsMinimalImeMetadata()
    {
        var info = ImeExports.CreateInquireInfoForTesting();

        Assert.Equal(0u, info.PrivateDataSize);
        Assert.True((info.Property & ImeConstants.ImePropKbdCharFirst) != 0);
        Assert.True((info.Property & ImeConstants.ImePropSpecialUi) != 0);
        Assert.True((info.Property & ImeConstants.ImePropUnicode) != 0);
        Assert.True((info.Property & ImeConstants.ImePropCandidateListStartsAtOne) != 0);
        Assert.Equal(0u, info.Property & ImeConstants.ImePropAtCaret);
        Assert.Equal(0u, info.Property & ImeConstants.ImePropCompleteOnUnselect);
        Assert.True((info.ConversionCaps & ImeConstants.ImeCmodeNative) != 0);
        Assert.Equal(0u, info.ConversionCaps & ImeConstants.ImeCmodeNoConversion);
        Assert.Equal(ImeConstants.SCSCapsCompStr, info.SetCompositionStringCaps);
        Assert.Equal(0u, info.SelectCaps);
    }

    [Fact]
    public void ImeUiClassName_FitsImm32Buffer()
    {
        Assert.True(ImeExportsContract.ImeUiClassName.Length < ImeExportsContract.ImeUiClassBufferLength);
    }

    [Fact]
    public unsafe void ImeInquireManaged_WritesUiClassNameAndReturnsTrue()
    {
        var info = stackalloc ImeInquireInfo[1];
        var className = stackalloc char[ImeExportsContract.ImeUiClassBufferLength];

        var result = ImeExports.ImeInquireManaged(info, className, 0);

        Assert.Equal(1, result);
        Assert.Equal(ImeExportsContract.ImeUiClassName, new string(className));
    }

    [Fact]
    public unsafe void ImeInquireManaged_NullInquireInfoReturnsZero()
    {
        var result = ImeExports.ImeInquireManaged(null, null, 0);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Runtime_ShouldProcessVirtualKey_UsesTranslator()
    {
        Assert.True(ImeModuleRuntime.ShouldProcessVirtualKey(ImeConstants.VkA));
        Assert.False(ImeModuleRuntime.ShouldProcessVirtualKey(ImeConstants.VkTab));
    }

    [Fact]
    public unsafe void ImeProcessKeyManaged_ReturnsFalseForKeyUp()
    {
        var keyUpData = unchecked((nint)0x80000000);
        var processKeyCalled = false;
        ImeModuleRuntime.SetProcessKeyHandlerForTesting(_ =>
        {
            processKeyCalled = true;
            return new ImeProcessResult(ImeSessionSnapshot.Empty, null, true);
        });

        try
        {
            var result = ImeExports.ImeProcessKeyManaged(1, ImeConstants.VkA, keyUpData, null);

            Assert.False(result);
            Assert.False(processKeyCalled);
        }
        finally
        {
            ImeModuleRuntime.SetProcessKeyHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void ImeProcessKeyManaged_ReturnsZeroWhenRuntimeThrows()
    {
        ImeModuleRuntime.SetCompositionContextReaderForTesting(new ImeCompositionContextReader(new ThrowingImmContextAccessor()));

        try
        {
            var result = ImeExports.ImeProcessKeyManaged(1, ImeConstants.VkTab, 0, null);

            Assert.False(result);
        }
        finally
        {
            ImeModuleRuntime.SetCompositionContextReaderForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void ImeToAsciiExManaged_ReturnsZeroWhenRuntimeThrows()
    {
        ImeModuleRuntime.SetProcessKeyHandlerForTesting(_ => throw new InvalidOperationException("boom"));

        try
        {
            var result = ImeExports.ImeToAsciiExManaged(ImeConstants.VkA, 0, null, 0, 0, 0);

            Assert.Equal(0u, result);
        }
        finally
        {
            ImeModuleRuntime.SetProcessKeyHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void ImeSelectManaged_WhenDeselectedThenCancelsCompositionAndClearsHimc()
    {
        ImeKey? received = null;
        ImeModuleRuntime.SetProcessKeyHandlerForTesting(key =>
        {
            received = key;
            return new ImeProcessResult(ImeSessionSnapshot.Empty, null, true);
        });
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        var composition = himc.CompositionString;
        composition->Size = (uint)sizeof(CompositionString);
        composition->CompStrOffset = (uint)sizeof(CompositionString);
        composition->CompStrLength = 4;

        try
        {
            var result = ImeExports.ImeSelectManaged(himc.Handle, 0);

            Assert.Equal(1, result);
            Assert.Equal(ImeKeyKind.Escape, received?.Kind);
            Assert.Equal(0u, himc.CompositionString->CompStrLength);
            Assert.Equal(0u, himc.CandidateInfo->Count);
            Assert.False(ImeModuleRuntime.GetSnapshotForTesting(ImeSessionId.FromHimc(himc.Handle)).IsComposing);
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetProcessKeyHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void ImeSetActiveContextManaged_WhenDeactivatedThenCancelsCompositionAndClearsHimc()
    {
        ImeKey? received = null;
        ImeModuleRuntime.SetProcessKeyHandlerForTesting(key =>
        {
            received = key;
            return new ImeProcessResult(ImeSessionSnapshot.Empty, null, true);
        });
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        var composition = himc.CompositionString;
        composition->Size = (uint)sizeof(CompositionString);
        composition->CompStrOffset = (uint)sizeof(CompositionString);
        composition->CompStrLength = 4;

        try
        {
            var result = ImeExports.ImeSetActiveContextManaged(himc.Handle, 0);

            Assert.Equal(1, result);
            Assert.Equal(ImeKeyKind.Escape, received?.Kind);
            Assert.Equal(0u, himc.CompositionString->CompStrLength);
            Assert.Equal(0u, himc.CandidateInfo->Count);
            Assert.False(ImeModuleRuntime.GetSnapshotForTesting(ImeSessionId.FromHimc(himc.Handle)).IsComposing);
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetProcessKeyHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public void ImeDestroyManaged_WhenCalledThenResetsCachedSessions()
    {
        ImeModuleRuntime.SetProcessKeyHandlerForTesting(_ => new ImeProcessResult(ImeSessionSnapshot.Empty, null, true));
        ImeModuleRuntime.SetSnapshotForTesting(ImeSessionId.FromHimc(0x11), new ImeSessionSnapshot(
            new CompositionText("xiaoxiaimuyi", "xiaoxiaimuyi", 12),
            [new ImeCandidate("XiaoXiIme", "xiao xi ai mu yi")],
            new ImeCandidateWindowState(0, 0, 1),
            IsComposing: true));

        try
        {
            Assert.Equal(1, ImeExports.ImeDestroyManaged(0));
            Assert.False(ImeModuleRuntime.GetSnapshotForTesting(ImeSessionId.FromHimc(0x11)).IsComposing);
        }
        finally
        {
            ImeModuleRuntime.SetProcessKeyHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void ImeSetCompositionStringManaged_WhenScsSetStrThenWritesCompositionWithoutCommit()
    {
        string? received = null;
        ImeModuleRuntime.SetCompositionHandlerForTesting(composition =>
        {
            received = composition;
            return new ImeProcessResult(
                new ImeSessionSnapshot(
                    new CompositionText(composition, composition, composition.Length),
                    [new ImeCandidate("XiaoXiIme", "xiao xi ai mu yi")],
                    new ImeCandidateWindowState(0, 0, 1),
                    IsComposing: true),
                null,
                true);
        });
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        var composition = "xiaoxiaimuyi";
        fixed (char* compositionPointer = composition)
        {
            try
            {
                var result = ImeExports.ImeSetCompositionStringManaged(
                    himc.Handle,
                    ImeConstants.ScsSetStr,
                    compositionPointer,
                    (uint)(composition.Length * sizeof(char)),
                    null,
                    0);

                Assert.Equal(1, result);
                Assert.Equal(composition, received);
                Assert.Equal(composition, himc.ReadCompositionText());
                Assert.Equal("XiaoXiIme", himc.ReadFirstCandidateText());
                Assert.Equal(0u, himc.CompositionString->ResultStrLength);
            }
            finally
            {
                ImeExports.SetCompositionContextWriterForTesting(null);
                ImeModuleRuntime.SetCompositionHandlerForTesting(null);
                ImeModuleRuntime.SetBridgeForTesting(null);
            }
        }
    }

    [Fact]
    public unsafe void ImeSetCompositionStringManaged_WhenEmptyThenClearsHimcWithoutResultString()
    {
        ImeModuleRuntime.SetCompositionHandlerForTesting(_ => new ImeProcessResult(ImeSessionSnapshot.Empty, null, true));
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        var composition = himc.CompositionString;
        composition->Size = (uint)sizeof(CompositionString);
        composition->CompStrOffset = (uint)sizeof(CompositionString);
        composition->CompStrLength = 4;

        try
        {
            var result = ImeExports.ImeSetCompositionStringManaged(
                himc.Handle,
                ImeConstants.ScsSetStr,
                null,
                0,
                null,
                0);

            Assert.Equal(1, result);
            Assert.Equal(0u, himc.CompositionString->CompStrLength);
            Assert.Equal(0u, himc.CompositionString->ResultStrLength);
            Assert.Equal(0u, himc.CandidateInfo->Count);
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetCompositionHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void ImeSetCompositionStringManaged_WhenIndexIsUnsupportedThenReturnsZero()
    {
        var called = false;
        ImeModuleRuntime.SetCompositionHandlerForTesting(composition =>
        {
            called = true;
            return new ImeProcessResult(ImeSessionSnapshot.Empty, null, true);
        });
        var composition = "xiaoxiaimuyi";
        fixed (char* compositionPointer = composition)
        {
            try
            {
                var result = ImeExports.ImeSetCompositionStringManaged(
                    1,
                    ImeConstants.SCSCapsMakeRead,
                    compositionPointer,
                    (uint)(composition.Length * sizeof(char)),
                    null,
                    0);

                Assert.Equal(0, result);
                Assert.False(called);
            }
            finally
            {
                ImeModuleRuntime.SetCompositionHandlerForTesting(null);
                ImeModuleRuntime.SetBridgeForTesting(null);
            }
        }
    }

    [Fact]
    public unsafe void ImeConversionListManaged_WhenGclConversionThenWritesCandidateListWithoutChangingComposition()
    {
        ImeModuleRuntime.SetConversionListHandlerForTesting(_ => [new ImeCandidate("XiaoXiIme", "xiao xi ai mu yi")]);
        var source = "xiaoxiaimuyi";
        var buffer = new byte[256];
        fixed (char* sourcePointer = source)
        fixed (byte* destination = buffer)
        {
            try
            {
                var required = ImeExports.ImeConversionListManaged(
                    1,
                    sourcePointer,
                    null,
                    0,
                    ImeConstants.GclConversion);
                Assert.True(required > 0);

                var written = ImeExports.ImeConversionListManaged(
                    1,
                    sourcePointer,
                    destination,
                    (uint)buffer.Length,
                    ImeConstants.GclConversion);
                Assert.Equal(required, written);

                var candidateList = (CandidateList*)destination;
                Assert.Equal(1u, candidateList->Count);
                Assert.Equal("XiaoXiIme", new string((char*)(destination + candidateList->Offset[0])));
            }
            finally
            {
                ImeModuleRuntime.SetConversionListHandlerForTesting(null);
                ImeModuleRuntime.SetBridgeForTesting(null);
            }
        }
    }

    [Fact]
    public unsafe void ImeConversionListManaged_WhenGclReverseConversionThenWritesCanonicalReading()
    {
        var conversionCalled = false;
        ImeModuleRuntime.SetConversionListHandlerForTesting(_ =>
        {
            conversionCalled = true;
            return [new ImeCandidate("XiaoXiIme", "xx")];
        });
        ImeModuleRuntime.SetReverseConversionListHandlerForTesting(_ => [new ImeCandidate("XiaoXiIme", "xiao xi ai mu yi")]);
        var source = "XiaoXiIme";
        var buffer = new byte[256];
        fixed (char* sourcePointer = source)
        fixed (byte* destination = buffer)
        {
            try
            {
                var required = ImeExports.ImeConversionListManaged(
                    1,
                    sourcePointer,
                    null,
                    0,
                    ImeConstants.GclReverseConversion);
                Assert.True(required > 0);

                var written = ImeExports.ImeConversionListManaged(
                    1,
                    sourcePointer,
                    destination,
                    (uint)buffer.Length,
                    ImeConstants.GclReverseConversion);
                Assert.Equal(required, written);

                var candidateList = (CandidateList*)destination;
                Assert.Equal(1u, candidateList->Count);
                Assert.Equal("xiao xi ai mu yi", new string((char*)(destination + candidateList->Offset[0])));
                Assert.False(conversionCalled);
            }
            finally
            {
                ImeModuleRuntime.SetReverseConversionListHandlerForTesting(null);
                ImeModuleRuntime.SetConversionListHandlerForTesting(null);
                ImeModuleRuntime.SetBridgeForTesting(null);
            }
        }
    }

    [Fact]
    public unsafe void ImeConversionListManaged_WhenGclReverseLengthThenReturnsRequiredSizeWithoutWritingDestination()
    {
        var conversionCalled = false;
        ImeModuleRuntime.SetConversionListHandlerForTesting(_ =>
        {
            conversionCalled = true;
            return [new ImeCandidate("XiaoXiIme", "xx")];
        });
        ImeModuleRuntime.SetReverseConversionListHandlerForTesting(_ => [new ImeCandidate("XiaoXiIme", "xiao xi ai mu yi")]);
        var source = "XiaoXiIme";
        var buffer = new byte[256];
        Array.Fill(buffer, (byte)0xCC);
        fixed (char* sourcePointer = source)
        fixed (byte* destination = buffer)
        {
            try
            {
                var required = ImeExports.ImeConversionListManaged(
                    1,
                    sourcePointer,
                    destination,
                    (uint)buffer.Length,
                    ImeConstants.GclReverseLength);

                Assert.True(required > 0);
                Assert.Equal(
                    ImeCompositionContextWriter.GetRequiredConversionListSize(
                        [new ImeCandidate("XiaoXiIme", "xiao xi ai mu yi")],
                        writeReading: true),
                    required);
                Assert.All(buffer, value => Assert.Equal((byte)0xCC, value));
                Assert.False(conversionCalled);
            }
            finally
            {
                ImeModuleRuntime.SetReverseConversionListHandlerForTesting(null);
                ImeModuleRuntime.SetConversionListHandlerForTesting(null);
                ImeModuleRuntime.SetBridgeForTesting(null);
            }
        }
    }

    [Fact]
    public unsafe void ImeConversionListManaged_WhenFlagIsUnsupportedThenReturnsZero()
    {
        var conversionCalled = false;
        var reverseCalled = false;
        ImeModuleRuntime.SetConversionListHandlerForTesting(_ =>
        {
            conversionCalled = true;
            return [new ImeCandidate("XiaoXiIme", "xiao xi ai mu yi")];
        });
        ImeModuleRuntime.SetReverseConversionListHandlerForTesting(_ =>
        {
            reverseCalled = true;
            return [new ImeCandidate("XiaoXiIme", "xiao xi ai mu yi")];
        });
        var source = "xiaoxiaimuyi";
        fixed (char* sourcePointer = source)
        {
            try
            {
                var result = ImeExports.ImeConversionListManaged(
                    1,
                    sourcePointer,
                    null,
                    0,
                    ImeConstants.NiChangeCandidateList);

                Assert.Equal(0u, result);
                Assert.False(conversionCalled);
                Assert.False(reverseCalled);
            }
            finally
            {
                ImeModuleRuntime.SetReverseConversionListHandlerForTesting(null);
                ImeModuleRuntime.SetConversionListHandlerForTesting(null);
                ImeModuleRuntime.SetBridgeForTesting(null);
            }
        }
    }

    [Fact]
    public void ImeConstants_MatchWindowsSdkNotifyImeActions()
    {
        Assert.Equal(0x0012u, ImeConstants.NiSelectCandidateStr);
        Assert.Equal(0x0013u, ImeConstants.NiChangeCandidateList);
        Assert.Equal(0x0015u, ImeConstants.NiCompositionStr);
        Assert.Equal(0x0003u, ImeConstants.GclReverseLength);
    }

    [Fact]
    public unsafe void ImeEscapeManaged_WhenQuerySupportThenReportsImeName()
    {
        uint requested = ImeConstants.ImeEscImeName;
        var result = ImeExports.ImeEscapeManaged(0, ImeConstants.ImeEscQuerySupport, &requested);

        Assert.Equal((nint)1, result);
    }

    [Fact]
    public unsafe void ImeEscapeManaged_WhenQuerySupportThenRejectsUnsupportedEscape()
    {
        uint requested = ImeConstants.GclConversion;
        var result = ImeExports.ImeEscapeManaged(0, ImeConstants.ImeEscQuerySupport, &requested);

        Assert.Equal((nint)0, result);
    }

    [Fact]
    public unsafe void ImeEscapeManaged_WhenImeNameThenWritesDisplayName()
    {
        var name = stackalloc char[ImeConstants.ImeNameBufferLength];
        var result = ImeExports.ImeEscapeManaged(0, ImeConstants.ImeEscImeName, name);

        Assert.Equal((nint)1, result);
        Assert.Equal(ImeExportsContract.ImeDisplayName, new string(name));
    }

    [Fact]
    public unsafe void ImeEscapeManaged_WhenEscapeIsUnsupportedThenReturnsZero()
    {
        var result = ImeExports.ImeEscapeManaged(0, ImeConstants.GclConversion, null);

        Assert.Equal((nint)0, result);
    }

    [Fact]
    public unsafe void ImeConfigureManaged_WhenRegisterWordThenRegistersWithoutChangingComposition()
    {
        ImeRegisterWordRequest? received = null;
        ImeModuleRuntime.SetRegisterWordHandlerForTesting(request =>
        {
            received = request;
            return new ImeRegisterWordResponse(true, []);
        });
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        var composition = himc.CompositionString;
        composition->Size = (uint)sizeof(CompositionString);
        composition->CompStrOffset = (uint)sizeof(CompositionString);
        composition->CompStrLength = 24;
        var reading = "peizhi";
        var text = "配置词";

        try
        {
            fixed (char* readingPointer = reading)
            fixed (char* textPointer = text)
            {
                var registerWord = stackalloc RegisterWord[1];
                registerWord->Reading = readingPointer;
                registerWord->Word = textPointer;
                var result = ImeExports.ImeConfigureManaged(0, 0, ImeConstants.ImeConfigRegisterWord, registerWord);

                Assert.Equal(1, result);
                Assert.Equal(ImeRegisterWordAction.Register, received?.Action);
                Assert.Equal(reading, received?.Reading);
                Assert.Equal(text, received?.Text);
                Assert.Equal(24u, himc.CompositionString->CompStrLength);
                Assert.Equal(0u, himc.CompositionString->ResultStrLength);
            }
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetRegisterWordHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void ImeConfigureManaged_WhenModeIsUnsupportedThenReturnsZero()
    {
        var called = false;
        ImeModuleRuntime.SetRegisterWordHandlerForTesting(_ =>
        {
            called = true;
            return new ImeRegisterWordResponse(true, []);
        });

        try
        {
            var result = ImeExports.ImeConfigureManaged(0, 0, ImeConstants.ImeConfigGeneral, null);

            Assert.Equal(0, result);
            Assert.False(called);
        }
        finally
        {
            ImeModuleRuntime.SetRegisterWordHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void ImeGetRegisterWordStyleManaged_WhenBufferIsProvidedThenWritesUserStyle()
    {
        var style = stackalloc StyleBuf[1];
        var count = ImeExports.ImeGetRegisterWordStyleManaged(1, style);

        Assert.Equal(1u, count);
        Assert.Equal(ImeConstants.ImeRegWordStyleUserFirst, style->Style);
        Assert.Equal(ImeExportsContract.UserWordStyleDescription, new string(style->Description));
    }

    [Fact]
    public unsafe void ImeGetRegisterWordStyleManaged_WhenCountIsZeroThenReportsOneStyle()
    {
        var count = ImeExports.ImeGetRegisterWordStyleManaged(0, null);

        Assert.Equal(1u, count);
    }

    [Fact]
    public unsafe void ImeRegisterWordManaged_WhenUserStyleThenRegistersWithoutChangingComposition()
    {
        ImeRegisterWordRequest? received = null;
        ImeModuleRuntime.SetRegisterWordHandlerForTesting(request =>
        {
            received = request;
            return new ImeRegisterWordResponse(true, []);
        });
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        var composition = himc.CompositionString;
        composition->Size = (uint)sizeof(CompositionString);
        composition->CompStrOffset = (uint)sizeof(CompositionString);
        composition->CompStrLength = 24;
        var reading = "zidingyi";
        var text = "自定义";

        try
        {
            fixed (char* readingPointer = reading)
            fixed (char* textPointer = text)
            {
                var result = ImeExports.ImeRegisterWordManaged(readingPointer, ImeConstants.ImeRegWordStyleUserFirst, textPointer);

                Assert.Equal(1, result);
                Assert.Equal(ImeRegisterWordAction.Register, received?.Action);
                Assert.Equal(reading, received?.Reading);
                Assert.Equal(text, received?.Text);
                Assert.Equal(24u, himc.CompositionString->CompStrLength);
                Assert.Equal(0u, himc.CompositionString->ResultStrLength);
            }
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetRegisterWordHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void ImeRegisterWordManaged_WhenStyleIsUnsupportedThenReturnsZero()
    {
        var called = false;
        ImeModuleRuntime.SetRegisterWordHandlerForTesting(_ =>
        {
            called = true;
            return new ImeRegisterWordResponse(true, []);
        });
        var reading = "zidingyi";
        var text = "自定义";

        try
        {
            fixed (char* readingPointer = reading)
            fixed (char* textPointer = text)
            {
                var result = ImeExports.ImeRegisterWordManaged(readingPointer, ImeConstants.ImeRegWordStyleEudc, textPointer);

                Assert.Equal(0, result);
                Assert.False(called);
            }
        }
        finally
        {
            ImeModuleRuntime.SetRegisterWordHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void ImeUnregisterWordManaged_WhenUserStyleThenUnregistersWord()
    {
        ImeRegisterWordRequest? received = null;
        ImeModuleRuntime.SetRegisterWordHandlerForTesting(request =>
        {
            received = request;
            return new ImeRegisterWordResponse(true, []);
        });
        var reading = "zidingyi";
        var text = "自定义";

        try
        {
            fixed (char* readingPointer = reading)
            fixed (char* textPointer = text)
            {
                var result = ImeExports.ImeUnregisterWordManaged(readingPointer, 0, textPointer);

                Assert.Equal(1, result);
                Assert.Equal(ImeRegisterWordAction.Unregister, received?.Action);
                Assert.Equal(reading, received?.Reading);
                Assert.Equal(text, received?.Text);
            }
        }
        finally
        {
            ImeModuleRuntime.SetRegisterWordHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void ImeEnumRegisterWordManaged_WhenUserWordsExistThenInvokesCallback()
    {
        ImeModuleRuntime.SetRegisterWordHandlerForTesting(_ =>
            new ImeRegisterWordResponse(true, [new ImeRegisterWordEntry("zidingyi", "自定义")]));
        string? reading = null;
        string? text = null;
        uint style = 0;
        ImeExports.SetRegisterWordEnumProcForTesting((readingPointer, callbackStyle, textPointer, _) =>
        {
            reading = new string(readingPointer);
            text = new string(textPointer);
            style = callbackStyle;
            return 1;
        });

        try
        {
            var count = ImeExports.ImeEnumRegisterWordManaged(0, null, 0, null, null);

            Assert.Equal(1u, count);
            Assert.Equal("zidingyi", reading);
            Assert.Equal("自定义", text);
            Assert.Equal(ImeConstants.ImeRegWordStyleUserFirst, style);
        }
        finally
        {
            ImeExports.SetRegisterWordEnumProcForTesting(null);
            ImeModuleRuntime.SetRegisterWordHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void NotifyImeManaged_WhenCancelThenClearsCompositionWithoutCommit()
    {
        ImeKey? received = null;
        ImeModuleRuntime.SetProcessKeyHandlerForTesting(key =>
        {
            received = key;
            return new ImeProcessResult(ImeSessionSnapshot.Empty, null, true);
        });
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        var composition = himc.CompositionString;
        composition->Size = (uint)sizeof(CompositionString);
        composition->CompStrOffset = (uint)sizeof(CompositionString);
        composition->CompStrLength = 4;

        try
        {
            var result = ImeExports.NotifyImeManaged(himc.Handle, ImeConstants.NiCompositionStr, 0, ImeConstants.CpsCancel);

            Assert.Equal(1, result);
            Assert.Equal(ImeKeyKind.Escape, received?.Kind);
            Assert.Equal(0u, himc.CompositionString->CompStrLength);
            Assert.Equal(0u, himc.CompositionString->ResultStrLength);
            Assert.Equal(0u, himc.CandidateInfo->Count);
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetProcessKeyHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void NotifyImeManaged_WhenCompleteThenWritesResultString()
    {
        ImeKey? received = null;
        ImeModuleRuntime.SetProcessKeyHandlerForTesting(key =>
        {
            received = key;
            return new ImeProcessResult(ImeSessionSnapshot.Empty, "XiaoXiIme", true);
        });
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());

        try
        {
            var result = ImeExports.NotifyImeManaged(himc.Handle, ImeConstants.NiCompositionStr, 0, ImeConstants.CpsComplete);

            Assert.Equal(1, result);
            Assert.Equal(ImeKeyKind.Space, received?.Kind);
            Assert.Equal("XiaoXiIme", himc.ReadResultText());
            Assert.Equal(0u, himc.CompositionString->CompStrLength);
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetProcessKeyHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void NotifyImeManaged_WhenRevertThenWritesReading()
    {
        ImeKey? received = null;
        ImeModuleRuntime.SetProcessKeyHandlerForTesting(key =>
        {
            received = key;
            return new ImeProcessResult(ImeSessionSnapshot.Empty, "xiaoxiaimuyi", true);
        });
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());

        try
        {
            var result = ImeExports.NotifyImeManaged(himc.Handle, ImeConstants.NiCompositionStr, 0, ImeConstants.CpsRevert);

            Assert.Equal(1, result);
            Assert.Equal(ImeKeyKind.Enter, received?.Kind);
            Assert.Equal("xiaoxiaimuyi", himc.ReadResultText());
            Assert.Equal(0u, himc.CompositionString->CompStrLength);
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetProcessKeyHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void NotifyImeManaged_WhenSelectCandidateThenWritesResultString()
    {
        ImeKey? received = null;
        ImeModuleRuntime.SetProcessKeyHandlerForTesting(key =>
        {
            received = key;
            return new ImeProcessResult(ImeSessionSnapshot.Empty, "XiaoXiIme", true);
        });
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());

        try
        {
            var result = ImeExports.NotifyImeManaged(himc.Handle, ImeConstants.NiSelectCandidateStr, 0, 0);

            Assert.Equal(1, result);
            Assert.Equal(ImeKeyKind.CandidateSelection, received?.Kind);
            Assert.Equal(0, received?.CandidateIndex);
            Assert.Equal("XiaoXiIme", himc.ReadResultText());
            Assert.Equal(0u, himc.CompositionString->CompStrLength);
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetProcessKeyHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void ImeProcessKeyManaged_WhenVirtualKeyContainsCharacterThenUsesLowWordVirtualKey()
    {
        const uint virtualKeyWithCharacter = 0x00780058;
        ImeKeystrokeDiagnostics.Reset();

        var handled = ImeExports.ImeProcessKeyManaged(0, virtualKeyWithCharacter, 0, null);
        var diagnostics = ImeKeystrokeDiagnostics.GetSnapshot();

        Assert.True(handled);
        Assert.Equal(0x58u, diagnostics.LastProcessVirtualKey);
    }

    [Fact]
    public unsafe void ImeToAsciiExManaged_WhenVirtualKeyContainsCharacterThenUsesLowWordVirtualKey()
    {
        const uint virtualKeyWithCharacter = 0x00780058;
        ImeKey? received = null;
        ImeModuleRuntime.SetProcessKeyHandlerForTesting(key =>
        {
            received = key;
            return new ImeProcessResult(ImeSessionSnapshot.Empty, null, true);
        });
        ImeKeystrokeDiagnostics.Reset();

        try
        {
            var result = ImeExports.ImeToAsciiExManaged(virtualKeyWithCharacter, 0, null, 0, 0, 0);
            var diagnostics = ImeKeystrokeDiagnostics.GetSnapshot();

            Assert.Equal(1u, result);
            Assert.Equal(ImeKeyKind.Character, received?.Kind);
            Assert.Equal('x', received?.Character);
            Assert.Equal(0x58u, diagnostics.LastToAsciiVirtualKey);
        }
        finally
        {
            ImeModuleRuntime.SetProcessKeyHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public unsafe void ImeToAsciiExManaged_WritesMessagesForComposingResult()
    {
        ImeModuleRuntime.SetProcessKeyHandlerForTesting(_ => new ImeProcessResult(
            new ImeSessionSnapshot(
                new CompositionText("x", "x", 1),
                Array.Empty<ImeCandidate>(),
                ImeCandidateWindowState.Empty,
                IsComposing: true,
                Guideline: new ImeGuideline(ImeGuidelineLevel.NoCandidate, "无候选：x")),
            CommitText: null,
            Handled: true));
        var buffer = stackalloc byte[sizeof(TransMsgList) + sizeof(TransMsg)];
        var list = (TransMsgList*)buffer;

        try
        {
            var result = ImeExports.ImeToAsciiExManaged(ImeConstants.VkA, 0, null, (nint)list, 0, 0);

            Assert.Equal(2u, result);
            Assert.Equal(2u, list->Count);
            Assert.Equal(ImeConstants.WmImeStartComposition, list->Message.Message);
            Assert.Equal(ImeConstants.WmImeComposition, (&list->Message)[1].Message);
        }
        finally
        {
            ImeModuleRuntime.SetProcessKeyHandlerForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public void Runtime_ShouldProcessVirtualKey_ReturnsTrueForUnknownKeyWhenComposing()
    {
        ImeModuleRuntime.SetSnapshotForTesting(new ImeSessionSnapshot(
            new CompositionText("x", "小", 1),
            Array.Empty<ImeCandidate>(),
            ImeCandidateWindowState.Empty,
            IsComposing: true));

        try
        {
            Assert.True(ImeModuleRuntime.ShouldProcessVirtualKey(ImeConstants.VkTab));
        }
        finally
        {
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public void BuildMessages_CommitText_ReturnsResultAndEndCompositionMessages()
    {
        var result = new ImeToAsciiResult(true, "小希", ImeSessionSnapshot.Empty);

        var messages = ImeTransMsgBuilder.BuildMessages(result);

        Assert.Equal(2, messages.Length);
        Assert.Equal(ImeConstants.WmImeComposition, messages[0].Message);
        Assert.Equal((nuint)'小', messages[0].WParam);
        Assert.Equal((nint)ImeConstants.GcsResultStr, messages[0].LParam);
        Assert.Equal(ImeConstants.WmImeEndComposition, messages[1].Message);
    }

    [Fact]
    public void BuildMessages_ComposingSnapshot_ReturnsStartAndCompositionMessages()
    {
        var snapshot = new ImeSessionSnapshot(
            new CompositionText("x", "小", 1),
            Array.Empty<ImeCandidate>(),
            ImeCandidateWindowState.Empty,
            IsComposing: true);
        var result = new ImeToAsciiResult(true, null, snapshot);

        var messages = ImeTransMsgBuilder.BuildMessages(result);

        Assert.Equal(2, messages.Length);
        Assert.Equal(ImeConstants.WmImeStartComposition, messages[0].Message);
        Assert.Equal(ImeConstants.WmImeComposition, messages[1].Message);
        Assert.Equal(
            (nint)(ImeConstants.GcsCompStr
                | ImeConstants.GcsCompReadStr
                | ImeConstants.GcsCursorPos
                | ImeConstants.GcsCandidateInfo
                | ImeConstants.GcsGuideLine
                | ImeConstants.GcsPrivate),
            messages[1].LParam);
    }

    [Fact]
    public unsafe void Write_CopiesMessagesToTransMsgList()
    {
        var messages = new[]
        {
            new TransMsg { Message = ImeConstants.WmImeStartComposition },
            new TransMsg { Message = ImeConstants.WmImeComposition, LParam = (nint)ImeConstants.GcsCompStr },
        };
        var buffer = stackalloc byte[sizeof(TransMsgList) + sizeof(TransMsg)];
        var list = (TransMsgList*)buffer;

        var written = ImeTransMsgWriter.Write((nint)list, messages);

        Assert.Equal(2u, written);
        Assert.Equal(2u, list->Count);
        Assert.Equal(ImeConstants.WmImeStartComposition, list->Message.Message);
        Assert.Equal(ImeConstants.WmImeComposition, (&list->Message)[1].Message);
    }

    [Fact]
    public unsafe void CompositionContextWriter_WriteCompositionString_PopulatesCompositionStringLayout()
    {
        var writer = new ImeCompositionContextWriter(new NoOpImmContextAccessor());
        var buffer = stackalloc byte[512];
        var compositionString = (CompositionString*)buffer;

        var written = writer.TryWriteCompositionStringForTesting(compositionString, "小西", 1);

        Assert.True(written);
        Assert.Equal(4u, compositionString->CompStrLength);
        Assert.Equal(1u, compositionString->CursorPos);
        Assert.Equal(4u, compositionString->CompReadStrLength);
        Assert.Equal(2u, compositionString->CompReadAttrLength);
        Assert.Equal(8u, compositionString->CompReadClauseLength);
        Assert.Equal(2u, compositionString->CompAttrLength);
        Assert.Equal(8u, compositionString->CompClauseLength);
        Assert.Equal("小西", new string((char*)(buffer + compositionString->CompStrOffset), 0, 2));
        Assert.Equal("小西", new string((char*)(buffer + compositionString->CompReadStrOffset), 0, 2));
        Assert.Equal(ImeConstants.AttrInput, buffer[compositionString->CompAttrOffset]);
        Assert.Equal(ImeConstants.AttrInput, buffer[compositionString->CompAttrOffset + 1]);
        Assert.Equal(ImeConstants.AttrInput, buffer[compositionString->CompReadAttrOffset]);
        Assert.Equal(ImeConstants.AttrInput, buffer[compositionString->CompReadAttrOffset + 1]);
        var clauses = (uint*)(buffer + compositionString->CompClauseOffset);
        Assert.Equal(0u, clauses[0]);
        Assert.Equal(4u, clauses[1]);
        var readingClauses = (uint*)(buffer + compositionString->CompReadClauseOffset);
        Assert.Equal(0u, readingClauses[0]);
        Assert.Equal(4u, readingClauses[1]);
    }

    [Fact]
    public unsafe void CompositionContextWriter_WriteCompositionString_PopulatesReadingLayout()
    {
        var writer = new ImeCompositionContextWriter(new NoOpImmContextAccessor());
        var buffer = stackalloc byte[512];
        var compositionString = (CompositionString*)buffer;

        var written = writer.TryWriteCompositionStringForTesting(compositionString, "小西", "xiao", 2);

        Assert.True(written);
        Assert.Equal(8u, compositionString->CompReadStrLength);
        Assert.Equal(4u, compositionString->CompReadAttrLength);
        Assert.Equal("xiao", new string((char*)(buffer + compositionString->CompReadStrOffset), 0, 4));
        Assert.Equal("小西", new string((char*)(buffer + compositionString->CompStrOffset), 0, 2));
    }

    [Fact]
    public unsafe void CompositionContextWriter_WriteResultString_PopulatesResultStringLayout()
    {
        var writer = new ImeCompositionContextWriter(new NoOpImmContextAccessor());
        var buffer = stackalloc byte[512];
        var compositionString = (CompositionString*)buffer;

        var written = writer.TryWriteResultStringForTesting(compositionString, "小希");

        Assert.True(written);
        Assert.Equal(4u, compositionString->ResultStrLength);
        Assert.Equal(8u, compositionString->ResultClauseLength);
        Assert.Equal("小希", new string((char*)(buffer + compositionString->ResultStrOffset), 0, 2));
        var clauses = (uint*)(buffer + compositionString->ResultClauseOffset);
        Assert.Equal(0u, clauses[0]);
        Assert.Equal(4u, clauses[1]);
    }

    [Fact]
    public unsafe void CompositionContextWriter_TryWrite_CommitClearsCandidateGuideLineAndPrivateData()
    {
        var compositionBuffer = stackalloc byte[512];
        var candidateBuffer = stackalloc byte[1024];
        var guideLineBuffer = stackalloc byte[512];
        var privateBuffer = stackalloc byte[128];
        var inputContext = new InputContext
        {
            HCompStr = 2,
            HCandInfo = 3,
            HGuideLine = 4,
            HPrivate = 5,
        };
        var accessor = new InMemoryImmContextAccessor(
            (nint)(&inputContext),
            (nint)compositionBuffer,
            2,
            (nint)candidateBuffer,
            3,
            (nint)guideLineBuffer,
            4,
            (nint)privateBuffer,
            5);
        var writer = new ImeCompositionContextWriter(accessor);
        var snapshot = new ImeSessionSnapshot(
            new CompositionText("xiao", "小", 1),
            new[] { new ImeCandidate("小", "xiao") },
            new ImeCandidateWindowState(0, 0, 1),
            IsComposing: true,
            new ImeGuideline(ImeGuidelineLevel.Reading, "xiao"));

        var written = writer.TryWrite(new HImc(1), new ImeToAsciiResult(true, "小", snapshot));

        Assert.True(written);
        var compositionString = (CompositionString*)compositionBuffer;
        Assert.Equal(2u, compositionString->ResultStrLength);
        var candidateInfo = (CandidateInfo*)candidateBuffer;
        Assert.Equal(0u, candidateInfo->Count);
        var guideLine = (GuideLine*)guideLineBuffer;
        Assert.Equal(ImeConstants.GuidelineLevelNone, guideLine->Level);
        var privateData = (ImePrivateData*)privateBuffer;
        Assert.Equal(0u, privateData->CandidateCount);
        Assert.Equal(0u, privateData->CompositionLength);
    }

    [Fact]
    public unsafe void CompositionContextWriter_TryWrite_ReturnsFalseWhenResizeFailsWithoutThrowing()
    {
        var inputContext = new InputContext
        {
            HCompStr = 2,
            HCandInfo = 3,
            HGuideLine = 4,
            HPrivate = 5,
        };
        var accessor = new InMemoryImmContextAccessor((nint)(&inputContext), 0, 2)
        {
            FailResize = true,
        };
        var writer = new ImeCompositionContextWriter(accessor);
        var snapshot = new ImeSessionSnapshot(new CompositionText("x", "小", 1), [], ImeCandidateWindowState.Empty, IsComposing: true);

        var written = writer.TryWrite(new HImc(1), new ImeToAsciiResult(true, null, snapshot));

        Assert.False(written);
    }

    [Fact]
    public unsafe void CompositionContextWriter_WriteCandidateList_PopulatesCandidateInfoLayout()
    {
        var writer = new ImeCompositionContextWriter(new NoOpImmContextAccessor());
        var buffer = stackalloc byte[1024];
        var candidateInfo = (CandidateInfo*)buffer;
        var candidates = new[]
        {
            new ImeCandidate("小", "xiao"),
            new ImeCandidate("西", "xi"),
        };

        var written = writer.TryWriteCandidateListForTesting(candidateInfo, candidates, new ImeCandidateWindowState(1, 0, 9));

        Assert.True(written);
        Assert.Equal(1u, candidateInfo->Count);
        Assert.Equal((uint)sizeof(CandidateInfo), candidateInfo->Offset[0]);
        var candidateList = (CandidateList*)(buffer + candidateInfo->Offset[0]);
        Assert.Equal(2u, candidateList->Count);
        Assert.Equal(1u, candidateList->Selection);
        Assert.Equal(2u, candidateList->PageSize);
        Assert.Equal(ImeConstants.CandidateListStyleReading, candidateList->Style);
        Assert.Equal("小", new string((char*)((byte*)candidateList + candidateList->Offset[0])));
        Assert.Equal("西", new string((char*)((byte*)candidateList + candidateList->Offset[1])));
    }

    [Fact]
    public unsafe void CompositionContextWriter_WriteCandidateList_PaginatesSelection()
    {
        var writer = new ImeCompositionContextWriter(new NoOpImmContextAccessor());
        var buffer = stackalloc byte[2048];
        var candidateInfo = (CandidateInfo*)buffer;
        var candidates = Enumerable.Range(0, 12)
            .Select(index => new ImeCandidate($"候选{index}", $"h{index}"))
            .ToArray();

        var written = writer.TryWriteCandidateListForTesting(candidateInfo, candidates, new ImeCandidateWindowState(10, 9, 9));

        Assert.True(written);
        var candidateList = (CandidateList*)(buffer + candidateInfo->Offset[0]);
        Assert.Equal(10u, candidateList->Selection);
        Assert.Equal(9u, candidateList->PageStart);
        Assert.Equal(3u, candidateList->PageSize);
    }

    [Fact]
    public unsafe void CompositionContextWriter_WriteCandidateList_NormalizesSelectionOutsidePage()
    {
        var writer = new ImeCompositionContextWriter(new NoOpImmContextAccessor());
        var buffer = stackalloc byte[2048];
        var candidateInfo = (CandidateInfo*)buffer;
        var candidates = Enumerable.Range(0, 12)
            .Select(index => new ImeCandidate($"候选{index}", $"h{index}"))
            .ToArray();

        var written = writer.TryWriteCandidateListForTesting(candidateInfo, candidates, new ImeCandidateWindowState(10, 0, 9));

        Assert.True(written);
        var candidateList = (CandidateList*)(buffer + candidateInfo->Offset[0]);
        Assert.Equal(10u, candidateList->Selection);
        Assert.Equal(9u, candidateList->PageStart);
        Assert.Equal(3u, candidateList->PageSize);
    }

    [Fact]
    public unsafe void CompositionContextWriter_WriteGuideLine_PopulatesEmptyGuidelineLayout()
    {
        var writer = new ImeCompositionContextWriter(new NoOpImmContextAccessor());
        var buffer = stackalloc byte[512];
        var guideLine = (GuideLine*)buffer;
        var snapshot = ImeSessionSnapshot.Empty;

        var written = writer.TryWriteGuideLineForTesting(guideLine, snapshot);

        Assert.True(written);
        Assert.Equal((uint)(sizeof(GuideLine) + sizeof(ImePrivateData)), guideLine->Size);
        Assert.Equal(ImeConstants.GuidelineLevelNone, guideLine->Level);
        Assert.Equal(ImeConstants.GuidelineIndexNone, guideLine->Index);
        Assert.Equal(0u, guideLine->StringLength);
        Assert.Equal((uint)sizeof(ImePrivateData), guideLine->PrivateSize);
        Assert.Equal((uint)sizeof(GuideLine), guideLine->PrivateOffset);
        var privateData = (ImePrivateData*)(buffer + guideLine->PrivateOffset);
        Assert.Equal(ImeConstants.XiaoXiImePrivateDataVersion, privateData->Version);
    }

    [Fact]
    public unsafe void CompositionContextWriter_WriteGuideLine_PopulatesReadingTextAndPrivateState()
    {
        var writer = new ImeCompositionContextWriter(new NoOpImmContextAccessor());
        var buffer = stackalloc byte[512];
        var guideLine = (GuideLine*)buffer;
        var snapshot = new ImeSessionSnapshot(
            new CompositionText("xiao", "小", 1),
            new[] { new ImeCandidate("小", "xiao") },
            new ImeCandidateWindowState(0, 0, 1),
            IsComposing: true,
            Guideline: new ImeGuideline(ImeGuidelineLevel.Reading, "xiao"));

        var written = writer.TryWriteGuideLineForTesting(guideLine, snapshot);

        Assert.True(written);
        Assert.Equal(ImeConstants.GuidelineLevelReading, guideLine->Level);
        Assert.Equal(8u, guideLine->StringLength);
        Assert.Equal((uint)sizeof(GuideLine), guideLine->StringOffset);
        Assert.Equal("xiao", new string((char*)(buffer + guideLine->StringOffset)));
        var privateData = (ImePrivateData*)(buffer + guideLine->PrivateOffset);
        Assert.Equal(ImeConstants.XiaoXiImePrivateDataVersion, privateData->Version);
        Assert.Equal(1u, privateData->CandidateCount);
        Assert.Equal(1u, privateData->CompositionLength);
        Assert.Equal(4u, privateData->ReadingLength);
        Assert.Equal(ImeConstants.GuidelineLevelReading, privateData->GuidelineLevel);
        Assert.Equal(1u, privateData->CandidateWindowVisible);
    }

    [Fact]
    public unsafe void CompositionContextWriter_WriteGuideLine_PopulatesNoCandidateSemanticText()
    {
        var writer = new ImeCompositionContextWriter(new NoOpImmContextAccessor());
        var buffer = stackalloc byte[512];
        var guideLine = (GuideLine*)buffer;
        var snapshot = new ImeSessionSnapshot(
            new CompositionText("x", "x", 1),
            Array.Empty<ImeCandidate>(),
            ImeCandidateWindowState.Empty,
            IsComposing: true,
            Guideline: new ImeGuideline(ImeGuidelineLevel.NoCandidate, "无候选：x"));

        var written = writer.TryWriteGuideLineForTesting(guideLine, snapshot);

        Assert.True(written);
        Assert.Equal(ImeConstants.GuidelineLevelNoCandidate, guideLine->Level);
        Assert.Equal("无候选：x", new string((char*)(buffer + guideLine->StringOffset)));
        var privateData = (ImePrivateData*)(buffer + guideLine->PrivateOffset);
        Assert.Equal(ImeConstants.GuidelineLevelNoCandidate, privateData->GuidelineLevel);
        Assert.Equal(0u, privateData->CandidateWindowVisible);
    }

    [Fact]
    public unsafe void CompositionContextWriter_WriteGuideLine_PopulatesInvalidInputSemanticText()
    {
        var writer = new ImeCompositionContextWriter(new NoOpImmContextAccessor());
        var buffer = stackalloc byte[512];
        var guideLine = (GuideLine*)buffer;
        var snapshot = new ImeSessionSnapshot(
            new CompositionText("!", "!", 1),
            Array.Empty<ImeCandidate>(),
            ImeCandidateWindowState.Empty,
            IsComposing: true,
            Guideline: new ImeGuideline(ImeGuidelineLevel.InvalidInput, "非法输入：!"));

        var written = writer.TryWriteGuideLineForTesting(guideLine, snapshot);

        Assert.True(written);
        Assert.Equal(ImeConstants.GuidelineLevelInvalidInput, guideLine->Level);
        Assert.Equal("非法输入：!", new string((char*)(buffer + guideLine->StringOffset)));
    }

    [Fact]
    public unsafe void CompositionContextWriter_WritePrivateData_PopulatesMinimalState()
    {
        var writer = new ImeCompositionContextWriter(new NoOpImmContextAccessor());
        var privateData = stackalloc ImePrivateData[1];
        var snapshot = new ImeSessionSnapshot(
            new CompositionText("xiao", "小西", 2),
            Enumerable.Range(0, 12).Select(index => new ImeCandidate($"候选{index}", $"h{index}")).ToArray(),
            new ImeCandidateWindowState(10, 9, 3),
            IsComposing: true);

        var written = writer.TryWritePrivateDataForTesting(privateData, snapshot);

        Assert.True(written);
        Assert.Equal((uint)sizeof(ImePrivateData), privateData->Size);
        Assert.Equal(ImeConstants.XiaoXiImePrivateDataVersion, privateData->Version);
        Assert.Equal(12u, privateData->CandidateCount);
        Assert.Equal(10u, privateData->CandidateSelection);
        Assert.Equal(9u, privateData->CandidatePageStart);
        Assert.Equal(3u, privateData->CandidatePageSize);
        Assert.Equal(2u, privateData->CompositionLength);
        Assert.Equal(4u, privateData->ReadingLength);
        Assert.Equal(ImeConstants.GuidelineLevelNone, privateData->GuidelineLevel);
        Assert.Equal(1u, privateData->CandidateWindowVisible);
    }

    [Fact]
    public unsafe void CompositionContextReader_IsCompositionStringActive_ReturnsTrueForActiveComposition()
    {
        var writer = new ImeCompositionContextWriter(new NoOpImmContextAccessor());
        var reader = new ImeCompositionContextReader(new NoOpImmContextAccessor());
        var buffer = stackalloc byte[512];
        var compositionString = (CompositionString*)buffer;
        writer.TryWriteCompositionStringForTesting(compositionString, "小西", 2);

        var isComposing = reader.IsCompositionStringActiveForTesting(compositionString);

        Assert.True(isComposing);
    }

    [Fact]
    public unsafe void CompositionContextReader_IsCompositionStringActive_ReturnsFalseForInvalidOffset()
    {
        var reader = new ImeCompositionContextReader(new NoOpImmContextAccessor());
        var compositionString = stackalloc CompositionString[1];
        compositionString->Size = (uint)sizeof(CompositionString);
        compositionString->CompStrOffset = (uint)sizeof(CompositionString) + 1u;
        compositionString->CompStrLength = 2;

        var isComposing = reader.IsCompositionStringActiveForTesting(compositionString);

        Assert.False(isComposing);
    }

    [Fact]
    public unsafe void Runtime_ShouldProcessVirtualKey_ReturnsTrueForUnknownKeyWhenInputContextIsComposing()
    {
        var compositionBuffer = stackalloc byte[512];
        var inputContext = new InputContext { HCompStr = 2 };
        var accessor = new InMemoryImmContextAccessor((nint)(&inputContext), (nint)compositionBuffer, 2);
        var writer = new ImeCompositionContextWriter(accessor);
        var compositionString = (CompositionString*)compositionBuffer;
        writer.TryWriteCompositionStringForTesting(compositionString, "小", 1);
        ImeModuleRuntime.SetCompositionContextReaderForTesting(new ImeCompositionContextReader(accessor));

        try
        {
            Assert.True(ImeModuleRuntime.ShouldProcessVirtualKey(ImeConstants.VkTab, 0, new HImc(1)));
        }
        finally
        {
            ImeModuleRuntime.SetCompositionContextReaderForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    private sealed class NoOpImmContextAccessor : IImmContextAccessor
    {
        public nint LockInputContext(HImc inputContext) => 0;

        public bool UnlockInputContext(HImc inputContext) => true;

        public nint LockCompositionString(nint compositionString) => 0;

        public bool UnlockCompositionString(nint compositionString) => true;

        public nint LockCandidateInfo(nint candidateInfo) => 0;

        public bool UnlockCandidateInfo(nint candidateInfo) => true;

        public nint LockGuideLine(nint guideLine) => 0;

        public bool UnlockGuideLine(nint guideLine) => true;

        public nint LockPrivateData(nint privateData) => 0;

        public bool UnlockPrivateData(nint privateData) => true;

        public nint ResizeCompositionString(nint compositionString, uint size) => compositionString;

        public nint ResizeCandidateInfo(nint candidateInfo, uint size) => candidateInfo;

        public nint ResizeGuideLine(nint guideLine, uint size) => guideLine;

        public nint ResizePrivateData(nint privateData, uint size) => privateData;

        public bool GenerateMessage(HImc inputContext) => true;
    }

    private sealed class ThrowingImmContextAccessor : IImmContextAccessor
    {
        public nint LockInputContext(HImc inputContext) => throw new InvalidOperationException("boom");
        public bool UnlockInputContext(HImc inputContext) => true;
        public nint LockCompositionString(nint compositionString) => 0;
        public bool UnlockCompositionString(nint compositionString) => true;
        public nint LockCandidateInfo(nint candidateInfo) => 0;
        public bool UnlockCandidateInfo(nint candidateInfo) => true;
        public nint LockGuideLine(nint guideLine) => 0;
        public bool UnlockGuideLine(nint guideLine) => true;
        public nint LockPrivateData(nint privateData) => 0;
        public bool UnlockPrivateData(nint privateData) => true;
        public nint ResizeCompositionString(nint compositionString, uint size) => 0;
        public nint ResizeCandidateInfo(nint candidateInfo, uint size) => 0;
        public nint ResizeGuideLine(nint guideLine, uint size) => 0;
        public nint ResizePrivateData(nint privateData, uint size) => 0;
        public bool GenerateMessage(HImc inputContext) => true;
    }
}
