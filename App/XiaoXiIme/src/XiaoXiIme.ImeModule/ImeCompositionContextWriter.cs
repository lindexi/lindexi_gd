using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

public sealed unsafe class ImeCompositionContextWriter
{
    private readonly IImmContextAccessor _contextAccessor;

    public ImeCompositionContextWriter(IImmContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public bool TryWrite(HImc inputContext, ImeToAsciiResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (inputContext.Value == 0 || !result.Handled)
        {
            return false;
        }

        var inputContextPointer = _contextAccessor.LockInputContext(inputContext);
        if (inputContextPointer == 0)
        {
            return false;
        }

        try
        {
            unsafe
            {
                var context = (InputContext*)inputContextPointer;
                if (!string.IsNullOrEmpty(result.CommitText))
                {
                    return TryWriteResultString(inputContext, context, result.CommitText)
                        & TryWriteCandidateInfo(inputContext, context, ImeSessionSnapshot.Empty)
                        & TryWriteGuideLine(inputContext, context, ImeSessionSnapshot.Empty)
                        & TryWritePrivateData(inputContext, context, ImeSessionSnapshot.Empty);
                }

                if (result.Snapshot.IsComposing)
                {
                    return TryWriteCompositionString(inputContext, context, result.Snapshot.Composition.DisplayText, result.Snapshot.Composition.Reading, result.Snapshot.Composition.CaretIndex)
                        & TryWriteCandidateInfo(inputContext, context, result.Snapshot)
                        & TryWriteGuideLine(inputContext, context, result.Snapshot)
                        & TryWritePrivateData(inputContext, context, result.Snapshot);
                }

                return TryClearCompositionString(inputContext, context)
                    & TryWriteCandidateInfo(inputContext, context, result.Snapshot)
                    & TryWriteGuideLine(inputContext, context, result.Snapshot)
                    & TryWritePrivateData(inputContext, context, result.Snapshot);
            }
        }
        finally
        {
            _contextAccessor.UnlockInputContext(inputContext);
        }
    }

    public bool TryWriteResultStringForTesting(CompositionString* compositionString, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return WriteResultString(compositionString, text);
    }

    public bool TryWriteCompositionStringForTesting(CompositionString* compositionString, string text, int cursorPos)
    {
        ArgumentNullException.ThrowIfNull(text);

        return WriteCompositionString(compositionString, text, text, cursorPos);
    }

    public bool TryWriteCompositionStringForTesting(CompositionString* compositionString, string text, string reading, int cursorPos)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(reading);

        return WriteCompositionString(compositionString, text, reading, cursorPos);
    }

    public bool TryWriteCandidateListForTesting(CandidateInfo* candidateInfo, IReadOnlyList<ImeCandidate> candidates, ImeCandidateWindowState? candidateWindow = null)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return WriteCandidateInfo(candidateInfo, candidates, candidateWindow ?? ImeCandidateWindowState.Empty);
    }

    public bool TryWriteGuideLineForTesting(GuideLine* guideLine, ImeSessionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return WriteGuideLine(guideLine, snapshot);
    }

    public bool TryWritePrivateDataForTesting(ImePrivateData* privateData, ImeSessionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return WritePrivateData(privateData, snapshot);
    }

    private bool TryWriteResultString(HImc inputContext, InputContext* context, string text)
    {
        var requiredSize = GetRequiredSize(resultText: text, compositionText: null);
        var compositionHandle = EnsureCompositionString(context, requiredSize);
        if (compositionHandle == 0)
        {
            return false;
        }

        var compositionPointer = _contextAccessor.LockCompositionString(compositionHandle);
        if (compositionPointer == 0)
        {
            return false;
        }

        try
        {
            if (!WriteResultString((CompositionString*)compositionPointer, text))
            {
                return false;
            }
        }
        finally
        {
            _contextAccessor.UnlockCompositionString(compositionHandle);
        }

        return _contextAccessor.GenerateMessage(inputContext);
    }

    private bool TryWriteCandidateInfo(HImc inputContext, InputContext* context, ImeSessionSnapshot snapshot)
    {
        var requiredSize = GetRequiredCandidateInfoSize(snapshot.Candidates);
        var candidateHandle = EnsureCandidateInfo(context, requiredSize);
        if (candidateHandle == 0)
        {
            return false;
        }

        var candidatePointer = _contextAccessor.LockCandidateInfo(candidateHandle);
        if (candidatePointer == 0)
        {
            return false;
        }

        try
        {
            if (!WriteCandidateInfo((CandidateInfo*)candidatePointer, snapshot.Candidates, snapshot.CandidateWindow))
            {
                return false;
            }
        }
        finally
        {
            _contextAccessor.UnlockCandidateInfo(candidateHandle);
        }

        return _contextAccessor.GenerateMessage(inputContext);
    }

    private bool TryWriteCompositionString(HImc inputContext, InputContext* context, string text, string reading, int cursorPos)
    {
        var requiredSize = GetRequiredSize(resultText: null, compositionText: text, compositionReading: reading);
        var compositionHandle = EnsureCompositionString(context, requiredSize);
        if (compositionHandle == 0)
        {
            return false;
        }

        var compositionPointer = _contextAccessor.LockCompositionString(compositionHandle);
        if (compositionPointer == 0)
        {
            return false;
        }

        try
        {
            if (!WriteCompositionString((CompositionString*)compositionPointer, text, reading, cursorPos))
            {
                return false;
            }
        }
        finally
        {
            _contextAccessor.UnlockCompositionString(compositionHandle);
        }

        return _contextAccessor.GenerateMessage(inputContext);
    }

    private bool TryWriteGuideLine(HImc inputContext, InputContext* context, ImeSessionSnapshot snapshot)
    {
        var requiredSize = GetRequiredGuideLineSize(snapshot);
        var guideLineHandle = EnsureGuideLine(context, requiredSize);
        if (guideLineHandle == 0)
        {
            return false;
        }

        var guideLinePointer = _contextAccessor.LockGuideLine(guideLineHandle);
        if (guideLinePointer == 0)
        {
            return false;
        }

        try
        {
            if (!WriteGuideLine((GuideLine*)guideLinePointer, snapshot))
            {
                return false;
            }
        }
        finally
        {
            _contextAccessor.UnlockGuideLine(guideLineHandle);
        }

        return _contextAccessor.GenerateMessage(inputContext);
    }

    private bool TryWritePrivateData(HImc inputContext, InputContext* context, ImeSessionSnapshot snapshot)
    {
        var requiredSize = (uint)Unsafe.SizeOf<ImePrivateData>();
        var privateHandle = EnsurePrivateData(context, requiredSize);
        if (privateHandle == 0)
        {
            return false;
        }

        var privatePointer = _contextAccessor.LockPrivateData(privateHandle);
        if (privatePointer == 0)
        {
            return false;
        }

        try
        {
            if (!WritePrivateData((ImePrivateData*)privatePointer, snapshot))
            {
                return false;
            }
        }
        finally
        {
            _contextAccessor.UnlockPrivateData(privateHandle);
        }

        return _contextAccessor.GenerateMessage(inputContext);
    }

    private bool TryClearCompositionString(HImc inputContext, InputContext* context)
    {
        var requiredSize = (uint)Unsafe.SizeOf<CompositionString>();
        var compositionHandle = EnsureCompositionString(context, requiredSize);
        if (compositionHandle == 0)
        {
            return false;
        }

        var compositionPointer = _contextAccessor.LockCompositionString(compositionHandle);
        if (compositionPointer == 0)
        {
            return false;
        }

        try
        {
            Unsafe.InitBlockUnaligned((void*)compositionPointer, 0, requiredSize);
            ((CompositionString*)compositionPointer)->Size = requiredSize;
        }
        finally
        {
            _contextAccessor.UnlockCompositionString(compositionHandle);
        }

        return _contextAccessor.GenerateMessage(inputContext);
    }

    private nint EnsureCompositionString(InputContext* context, uint requiredSize)
    {
        var compositionHandle = context->HCompStr;
        if (compositionHandle == 0)
        {
            return 0;
        }

        var resizedHandle = _contextAccessor.ResizeCompositionString(compositionHandle, requiredSize);
        if (resizedHandle == 0)
        {
            return 0;
        }

        context->HCompStr = resizedHandle;
        return resizedHandle;
    }

    private nint EnsureCandidateInfo(InputContext* context, uint requiredSize)
    {
        var candidateHandle = context->HCandInfo;
        if (candidateHandle == 0)
        {
            return 0;
        }

        var resizedHandle = _contextAccessor.ResizeCandidateInfo(candidateHandle, requiredSize);
        if (resizedHandle == 0)
        {
            return 0;
        }

        context->HCandInfo = resizedHandle;
        return resizedHandle;
    }

    private nint EnsureGuideLine(InputContext* context, uint requiredSize)
    {
        var guideLineHandle = context->HGuideLine;
        if (guideLineHandle == 0)
        {
            return 0;
        }

        var resizedHandle = _contextAccessor.ResizeGuideLine(guideLineHandle, requiredSize);
        if (resizedHandle == 0)
        {
            return 0;
        }

        context->HGuideLine = resizedHandle;
        return resizedHandle;
    }

    private nint EnsurePrivateData(InputContext* context, uint requiredSize)
    {
        var privateHandle = context->HPrivate;
        if (privateHandle == 0)
        {
            return 0;
        }

        var resizedHandle = _contextAccessor.ResizePrivateData(privateHandle, requiredSize);
        if (resizedHandle == 0)
        {
            return 0;
        }

        context->HPrivate = resizedHandle;
        return resizedHandle;
    }

    private static uint GetRequiredSize(string? resultText, string? compositionText, string? compositionReading = null)
    {
        checked
        {
            var size = (uint)Unsafe.SizeOf<CompositionString>();
            if (!string.IsNullOrEmpty(resultText))
            {
                size += (uint)Encoding.Unicode.GetByteCount(resultText);
                size += sizeof(uint) * 2u;
            }

            if (!string.IsNullOrEmpty(compositionText))
            {
                var byteCount = (uint)Encoding.Unicode.GetByteCount(compositionText);
                size += byteCount;
                size += byteCount == 0 ? 0u : byteCount / sizeof(char);
                size += sizeof(uint) * 2u;
            }

            if (!string.IsNullOrEmpty(compositionReading))
            {
                var byteCount = (uint)Encoding.Unicode.GetByteCount(compositionReading);
                size += byteCount;
                size += byteCount == 0 ? 0u : byteCount / sizeof(char);
                size += sizeof(uint) * 2u;
            }

            return size;
        }
    }

    private static uint GetRequiredGuideLineSize(ImeSessionSnapshot snapshot)
    {
        checked
        {
            var size = (uint)Unsafe.SizeOf<GuideLine>();
            var guidelineText = GetGuideLineText(snapshot);
            if (!string.IsNullOrEmpty(guidelineText))
            {
                size += (uint)Encoding.Unicode.GetByteCount(guidelineText) + sizeof(char);
            }

            size += (uint)Unsafe.SizeOf<ImePrivateData>();
            return size;
        }
    }

    private static uint GetRequiredCandidateInfoSize(IReadOnlyList<ImeCandidate> candidates)
    {
        checked
        {
            var size = (uint)Unsafe.SizeOf<CandidateInfo>();
            size += GetRequiredCandidateListSize(candidates);
            return size;
        }
    }

    private static uint GetRequiredCandidateListSize(IReadOnlyList<ImeCandidate> candidates)
    {
        checked
        {
            var count = (uint)candidates.Count;
            var size = (uint)Unsafe.SizeOf<CandidateList>();
            if (count > 1)
            {
                size += (count - 1u) * sizeof(uint);
            }

            foreach (var candidate in candidates)
            {
                size += (uint)Encoding.Unicode.GetByteCount(candidate.Text) + sizeof(char);
            }

            return size;
        }
    }

    private static bool WriteResultString(CompositionString* compositionString, string text)
    {
        var resultBytes = Encoding.Unicode.GetBytes(text);
        var requiredSize = GetRequiredSize(resultText: text, compositionText: null);
        ZeroCompositionString(compositionString, requiredSize);

        compositionString->Size = requiredSize;
        compositionString->ResultStrLength = (uint)resultBytes.Length;
        compositionString->ResultStrOffset = (uint)Unsafe.SizeOf<CompositionString>();
        compositionString->ResultClauseLength = sizeof(uint) * 2u;
        compositionString->ResultClauseOffset = compositionString->ResultStrOffset + compositionString->ResultStrLength;

        var target = (byte*)compositionString + compositionString->ResultStrOffset;
        resultBytes.CopyTo(new Span<byte>(target, resultBytes.Length));
        WriteClause((uint*)((byte*)compositionString + compositionString->ResultClauseOffset), compositionString->ResultStrLength);
        return true;
    }

    private static bool WriteCompositionString(CompositionString* compositionString, string text, string reading, int cursorPos)
    {
        var compositionBytes = Encoding.Unicode.GetBytes(text);
        var readingBytes = Encoding.Unicode.GetBytes(reading);
        var requiredSize = GetRequiredSize(resultText: null, compositionText: text, compositionReading: reading);
        ZeroCompositionString(compositionString, requiredSize);

        compositionString->Size = requiredSize;
        var offset = (uint)Unsafe.SizeOf<CompositionString>();
        compositionString->CompReadStrLength = (uint)readingBytes.Length;
        compositionString->CompReadStrOffset = offset;
        offset += compositionString->CompReadStrLength;
        compositionString->CompReadAttrLength = (uint)reading.Length;
        compositionString->CompReadAttrOffset = offset;
        offset += compositionString->CompReadAttrLength;
        compositionString->CompReadClauseLength = sizeof(uint) * 2u;
        compositionString->CompReadClauseOffset = offset;
        offset += compositionString->CompReadClauseLength;
        compositionString->CompStrLength = (uint)compositionBytes.Length;
        compositionString->CompStrOffset = offset;
        compositionString->CompAttrLength = (uint)text.Length;
        compositionString->CompAttrOffset = compositionString->CompStrOffset + compositionString->CompStrLength;
        compositionString->CompClauseLength = sizeof(uint) * 2u;
        compositionString->CompClauseOffset = compositionString->CompAttrOffset + compositionString->CompAttrLength;
        compositionString->CursorPos = (uint)Math.Clamp(cursorPos, 0, text.Length);
        compositionString->DeltaStart = 0;

        var readingTarget = (byte*)compositionString + compositionString->CompReadStrOffset;
        readingBytes.CopyTo(new Span<byte>(readingTarget, readingBytes.Length));
        var readingAttributes = new Span<byte>((byte*)compositionString + compositionString->CompReadAttrOffset, reading.Length);
        readingAttributes.Fill((byte)ImeConstants.AttrInput);
        WriteClause((uint*)((byte*)compositionString + compositionString->CompReadClauseOffset), compositionString->CompReadStrLength);
        var target = (byte*)compositionString + compositionString->CompStrOffset;
        compositionBytes.CopyTo(new Span<byte>(target, compositionBytes.Length));
        var attributes = new Span<byte>((byte*)compositionString + compositionString->CompAttrOffset, text.Length);
        attributes.Fill((byte)ImeConstants.AttrInput);
        WriteClause((uint*)((byte*)compositionString + compositionString->CompClauseOffset), compositionString->CompStrLength);
        return true;
    }

    private static bool WriteCandidateInfo(CandidateInfo* candidateInfo, IReadOnlyList<ImeCandidate> candidates, ImeCandidateWindowState candidateWindow)
    {
        var requiredSize = GetRequiredCandidateInfoSize(candidates);
        Unsafe.InitBlockUnaligned(candidateInfo, 0, requiredSize);

        candidateInfo->Size = requiredSize;
        candidateInfo->Count = candidates.Count == 0 ? 0u : 1u;
        if (candidates.Count == 0)
        {
            return true;
        }

        candidateInfo->Offset[0] = (uint)Unsafe.SizeOf<CandidateInfo>();
        var candidateList = (CandidateList*)((byte*)candidateInfo + candidateInfo->Offset[0]);
        var candidateListSize = GetRequiredCandidateListSize(candidates);
        candidateList->Size = candidateListSize;
        candidateList->Style = ImeConstants.CandidateListStyleReading;
        candidateList->Count = (uint)candidates.Count;
        candidateList->Selection = GetCandidateSelection(candidates.Count, candidateWindow.Selection);
        candidateList->PageStart = GetCandidatePageStart(candidates.Count, candidateWindow);
        candidateList->PageSize = GetCandidatePageSize(candidates.Count, candidateList->PageStart, candidateWindow.PageSize);

        var textOffset = (uint)Unsafe.SizeOf<CandidateList>();
        if (candidates.Count > 1)
        {
            textOffset += ((uint)candidates.Count - 1u) * sizeof(uint);
        }

        for (var i = 0; i < candidates.Count; i++)
        {
            candidateList->Offset[i] = textOffset;
            var candidateText = candidates[i].Text;
            var target = (char*)((byte*)candidateList + textOffset);
            candidateText.AsSpan().CopyTo(new Span<char>(target, candidateText.Length));
            target[candidateText.Length] = '\0';
            textOffset += (uint)((candidateText.Length + 1) * sizeof(char));
        }

        return true;
    }

    private static bool WriteGuideLine(GuideLine* guideLine, ImeSessionSnapshot snapshot)
    {
        var requiredSize = GetRequiredGuideLineSize(snapshot);
        Unsafe.InitBlockUnaligned(guideLine, 0, requiredSize);
        guideLine->Size = requiredSize;
        var guideline = snapshot.EffectiveGuideline;
        var guidelineText = GetGuideLineText(snapshot);
        guideLine->Level = ToGuideLineLevel(guideline.Level);
        guideLine->Index = ImeConstants.GuidelineIndexNone;

        var privateOffset = (uint)Unsafe.SizeOf<GuideLine>();
        if (!string.IsNullOrEmpty(guidelineText))
        {
            guideLine->StringLength = (uint)Encoding.Unicode.GetByteCount(guidelineText);
            guideLine->StringOffset = (uint)Unsafe.SizeOf<GuideLine>();
            var target = (char*)((byte*)guideLine + guideLine->StringOffset);
            guidelineText.AsSpan().CopyTo(new Span<char>(target, guidelineText.Length));
            target[guidelineText.Length] = '\0';
            privateOffset += guideLine->StringLength + sizeof(char);
        }

        guideLine->PrivateSize = (uint)Unsafe.SizeOf<ImePrivateData>();
        guideLine->PrivateOffset = privateOffset;
        return WritePrivateData((ImePrivateData*)((byte*)guideLine + guideLine->PrivateOffset), snapshot);
    }

    private static bool WritePrivateData(ImePrivateData* privateData, ImeSessionSnapshot snapshot)
    {
        var requiredSize = (uint)Unsafe.SizeOf<ImePrivateData>();
        Unsafe.InitBlockUnaligned(privateData, 0, requiredSize);
        privateData->Size = requiredSize;
        privateData->Version = ImeConstants.XiaoXiImePrivateDataVersion;
        privateData->CandidateCount = (uint)snapshot.Candidates.Count;
        privateData->CandidateSelection = GetCandidateSelection(snapshot.Candidates.Count, snapshot.CandidateWindow.Selection);
        privateData->CandidatePageStart = GetCandidatePageStart(snapshot.Candidates.Count, snapshot.CandidateWindow);
        privateData->CandidatePageSize = GetCandidatePageSize(snapshot.Candidates.Count, privateData->CandidatePageStart, snapshot.CandidateWindow.PageSize);
        privateData->CompositionLength = (uint)snapshot.Composition.DisplayText.Length;
        privateData->ReadingLength = (uint)snapshot.Composition.Reading.Length;
        privateData->GuidelineLevel = ToGuideLineLevel(snapshot.EffectiveGuideline.Level);
        privateData->CandidateWindowVisible = snapshot.IsComposing && snapshot.Candidates.Count > 0 ? 1u : 0u;
        return true;
    }

    private static string GetGuideLineText(ImeSessionSnapshot snapshot)
    {
        var guideline = snapshot.EffectiveGuideline;
        if (!string.IsNullOrEmpty(guideline.Text))
        {
            return guideline.Text;
        }

        return guideline.Level is ImeGuidelineLevel.None ? string.Empty : snapshot.Composition.Reading;
    }

    private static uint ToGuideLineLevel(ImeGuidelineLevel level)
    {
        return level switch
        {
            ImeGuidelineLevel.None => ImeConstants.GuidelineLevelNone,
            ImeGuidelineLevel.Info => ImeConstants.GuidelineLevelInfo,
            ImeGuidelineLevel.Warning => ImeConstants.GuidelineLevelWarning,
            ImeGuidelineLevel.Error => ImeConstants.GuidelineLevelError,
            ImeGuidelineLevel.Reading => ImeConstants.GuidelineLevelReading,
            ImeGuidelineLevel.NoCandidate => ImeConstants.GuidelineLevelNoCandidate,
            ImeGuidelineLevel.InvalidInput => ImeConstants.GuidelineLevelInvalidInput,
            _ => ImeConstants.GuidelineLevelInfo,
        };
    }

    private static uint GetCandidateSelection(int candidateCount, int selection)
    {
        if (candidateCount <= 0)
        {
            return 0;
        }

        return (uint)Math.Clamp(selection, 0, candidateCount - 1);
    }

    private static uint GetCandidatePageStart(int candidateCount, ImeCandidateWindowState candidateWindow)
    {
        if (candidateCount <= 0)
        {
            return 0;
        }

        var normalizedSelection = (int)GetCandidateSelection(candidateCount, candidateWindow.Selection);
        var pageSize = Math.Clamp(candidateWindow.PageSize, 1, candidateCount);
        var pageStart = Math.Clamp(candidateWindow.PageStart, 0, candidateCount - 1);
        if (normalizedSelection < pageStart || normalizedSelection >= pageStart + pageSize)
        {
            pageStart = normalizedSelection / pageSize * pageSize;
        }

        return (uint)pageStart;
    }

    private static uint GetCandidatePageSize(int candidateCount, uint pageStart, int pageSize)
    {
        if (candidateCount <= 0)
        {
            return 0;
        }

        var normalizedPageSize = Math.Clamp(pageSize, 1, (int)ImeConstants.CandidatePageSize);
        return Math.Min((uint)candidateCount - pageStart, (uint)normalizedPageSize);
    }

    private static void ZeroCompositionString(CompositionString* compositionString, uint size)
    {
        Unsafe.InitBlockUnaligned(compositionString, 0, size);
    }

    private static void WriteClause(uint* clause, uint byteLength)
    {
        clause[0] = ImeConstants.ClauseStart;
        clause[1] = byteLength;
    }
}
