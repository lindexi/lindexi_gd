using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule.Tests;

internal sealed class InMemoryImmContextAccessor : IImmContextAccessor
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

    public InMemoryImmContextAccessor(
        nint inputContext,
        nint compositionString,
        nint compositionHandle,
        nint candidateInfo,
        nint candidateHandle,
        nint guideLine,
        nint guideLineHandle,
        nint privateData,
        nint privateHandle)
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

    public nint LockCompositionString(nint compositionString) =>
        compositionString == _compositionHandle ? _compositionString : 0;

    public bool UnlockCompositionString(nint compositionString) => true;

    public nint LockCandidateInfo(nint candidateInfo) =>
        candidateInfo == _candidateHandle ? _candidateInfo : 0;

    public bool UnlockCandidateInfo(nint candidateInfo) => true;

    public nint LockGuideLine(nint guideLine) =>
        guideLine == _guideLineHandle ? _guideLine : 0;

    public bool UnlockGuideLine(nint guideLine) => true;

    public nint LockPrivateData(nint privateData) =>
        privateData == _privateHandle ? _privateData : 0;

    public bool UnlockPrivateData(nint privateData) => true;

    public nint ResizeCompositionString(nint compositionString, uint size) => FailResize ? 0 : compositionString;

    public nint ResizeCandidateInfo(nint candidateInfo, uint size) => FailResize ? 0 : candidateInfo;

    public nint ResizeGuideLine(nint guideLine, uint size) => FailResize ? 0 : guideLine;

    public nint ResizePrivateData(nint privateData, uint size) => FailResize ? 0 : privateData;

    public bool GenerateMessage(HImc inputContext) => true;
}
