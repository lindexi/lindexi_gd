using XiaoXiIme.ImeInterop;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeModule.Tests;

public class ImeExportsTests
{
    [Fact]
    public void CreateInquireInfoForTesting_ReturnsMinimalImeMetadata()
    {
        var info = ImeExports.CreateInquireInfoForTesting();

        Assert.NotEqual(0u, info.Size);
        Assert.Equal(ImeConstants.ImeVersion0400, info.ImeVersion);
        Assert.True((info.ImeProperty & ImeConstants.ImePropUnicode) != 0);
        Assert.True((info.ImeProperty & ImeConstants.ImePropAtCaret) != 0);
        Assert.True((info.ImeProperty & ImeConstants.ImePropCompleteOnUnselect) != 0);
        Assert.True((info.ConversionCaps & ImeConstants.ImeCmodeNative) != 0);
        Assert.True((info.SetCompositionStringCaps & ImeConstants.SCSCapsMakeRead) != 0);
        Assert.True((info.SelectCaps & ImeConstants.SelectCapsConversion) != 0);
    }

    [Fact]
    public unsafe void ImeInquireManaged_WritesClassNamesAndReturnsSize()
    {
        var info = stackalloc ImeInquireInfo[1];
        var className = stackalloc char[80];

        var size = ImeExports.ImeInquireManaged(info, className, 0);

        Assert.Equal(info->Size, size);
        Assert.Equal(ImeExportsContract.ImeMenuClassName, new string(info->ImeMenuClassName));

        Assert.Equal(ImeExportsContract.ImeUiClassName, new string(className));
    }

    [Fact]
    public unsafe void ImeInquireManaged_NullInquireInfoReturnsZero()
    {
        var result = ImeExports.ImeInquireManaged(null, null, 0);

        Assert.Equal(0u, result);
    }

    [Fact]
    public void Runtime_ShouldProcessVirtualKey_UsesTranslator()
    {
        Assert.True(ImeModuleRuntime.ShouldProcessVirtualKey(ImeConstants.VkA));
        Assert.False(ImeModuleRuntime.ShouldProcessVirtualKey(ImeConstants.VkTab));
    }

    [Fact]
    public unsafe void ImeProcessKeyManaged_ReturnsZeroWhenRuntimeThrows()
    {
        ImeModuleRuntime.SetCompositionContextReaderForTesting(new ImeCompositionContextReader(new ThrowingImmContextAccessor()));

        try
        {
            var result = ImeExports.ImeProcessKeyManaged(1, ImeConstants.VkTab, 0, null);

            Assert.Equal(0u, result);
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
        var buffer = stackalloc byte[sizeof(uint) + (sizeof(TransMsg) * 2)];
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
        var result = new ImeToAsciiResult(true, "小", ImeSessionSnapshot.Empty);

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
        var buffer = stackalloc byte[sizeof(uint) + (sizeof(TransMsg) * 2)];
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

        var written = writer.TryWriteResultStringForTesting(compositionString, "小");

        Assert.True(written);
        Assert.Equal(2u, compositionString->ResultStrLength);
        Assert.Equal(8u, compositionString->ResultClauseLength);
        Assert.Equal("小", new string((char*)(buffer + compositionString->ResultStrOffset), 0, 1));
        var clauses = (uint*)(buffer + compositionString->ResultClauseOffset);
        Assert.Equal(0u, clauses[0]);
        Assert.Equal(2u, clauses[1]);
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

    private sealed class InMemoryImmContextAccessor : IImmContextAccessor
    {
        private readonly nint _inputContext;
        private readonly nint _compositionString;
        private readonly nint _compositionHandle;
        private readonly nint _candidateInfo;
        private readonly nint _candidateHandle;
        private readonly nint _guideLine;
        private readonly nint _guideLineHandle;
        private readonly nint _privateData;
        private readonly nint _privateHandle;

        public InMemoryImmContextAccessor(nint inputContext, nint compositionString, nint compositionHandle)
            : this(inputContext, compositionString, compositionHandle, 0, 0, 0, 0, 0, 0)
        {
        }

        public InMemoryImmContextAccessor(nint inputContext, nint compositionString, nint compositionHandle, nint candidateInfo, nint candidateHandle, nint guideLine, nint guideLineHandle, nint privateData, nint privateHandle)
        {
            _inputContext = inputContext;
            _compositionString = compositionString;
            _compositionHandle = compositionHandle;
            _candidateInfo = candidateInfo;
            _candidateHandle = candidateHandle;
            _guideLine = guideLine;
            _guideLineHandle = guideLineHandle;
            _privateData = privateData;
            _privateHandle = privateHandle;
        }

        public bool FailResize { get; init; }

        public nint LockInputContext(HImc inputContext) => inputContext.Value == 0 ? 0 : _inputContext;

        public bool UnlockInputContext(HImc inputContext) => true;
        public nint LockCompositionString(nint compositionString) => compositionString == _compositionHandle ? _compositionString : 0;
        public bool UnlockCompositionString(nint compositionString) => true;
        public nint LockCandidateInfo(nint candidateInfo) => candidateInfo == _candidateHandle ? _candidateInfo : 0;
        public bool UnlockCandidateInfo(nint candidateInfo) => true;
        public nint LockGuideLine(nint guideLine) => guideLine == _guideLineHandle ? _guideLine : 0;
        public bool UnlockGuideLine(nint guideLine) => true;
        public nint LockPrivateData(nint privateData) => privateData == _privateHandle ? _privateData : 0;
        public bool UnlockPrivateData(nint privateData) => true;
        public nint ResizeCompositionString(nint compositionString, uint size) => FailResize ? 0 : compositionString;
        public nint ResizeCandidateInfo(nint candidateInfo, uint size) => FailResize ? 0 : candidateInfo;
        public nint ResizeGuideLine(nint guideLine, uint size) => FailResize ? 0 : guideLine;
        public nint ResizePrivateData(nint privateData, uint size) => FailResize ? 0 : privateData;
        public bool GenerateMessage(HImc inputContext) => true;
    }
}
