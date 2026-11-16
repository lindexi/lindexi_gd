using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

public interface IImmContextAccessor
{
    nint LockInputContext(HImc inputContext);

    bool UnlockInputContext(HImc inputContext);

    nint LockCompositionString(nint compositionString);

    bool UnlockCompositionString(nint compositionString);

    nint LockCandidateInfo(nint candidateInfo);

    bool UnlockCandidateInfo(nint candidateInfo);

    nint LockGuideLine(nint guideLine);

    bool UnlockGuideLine(nint guideLine);

    nint LockPrivateData(nint privateData);

    bool UnlockPrivateData(nint privateData);

    nint ResizeCompositionString(nint compositionString, uint size);

    nint ResizeCandidateInfo(nint candidateInfo, uint size);

    nint ResizeGuideLine(nint guideLine, uint size);

    nint ResizePrivateData(nint privateData, uint size);

    bool GenerateMessage(HImc inputContext);
}