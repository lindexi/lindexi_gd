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

public sealed class ImmContextAccessor : IImmContextAccessor
{
    public static ImmContextAccessor Instance { get; } = new();

    private ImmContextAccessor()
    {
    }

    public nint LockInputContext(HImc inputContext)
    {
        return Imm32Methods.ImmLockIMC(inputContext);
    }

    public bool UnlockInputContext(HImc inputContext)
    {
        return Imm32Methods.ImmUnlockIMC(inputContext);
    }

    public nint LockCompositionString(nint compositionString)
    {
        return Imm32Methods.ImmLockIMCC(compositionString);
    }

    public bool UnlockCompositionString(nint compositionString)
    {
        return Imm32Methods.ImmUnlockIMCC(compositionString);
    }

    public nint LockCandidateInfo(nint candidateInfo)
    {
        return Imm32Methods.ImmLockIMCC(candidateInfo);
    }

    public bool UnlockCandidateInfo(nint candidateInfo)
    {
        return Imm32Methods.ImmUnlockIMCC(candidateInfo);
    }

    public nint LockGuideLine(nint guideLine)
    {
        return Imm32Methods.ImmLockIMCC(guideLine);
    }

    public bool UnlockGuideLine(nint guideLine)
    {
        return Imm32Methods.ImmUnlockIMCC(guideLine);
    }

    public nint LockPrivateData(nint privateData)
    {
        return Imm32Methods.ImmLockIMCC(privateData);
    }

    public bool UnlockPrivateData(nint privateData)
    {
        return Imm32Methods.ImmUnlockIMCC(privateData);
    }

    public nint ResizeCompositionString(nint compositionString, uint size)
    {
        return Imm32Methods.ImmReSizeIMCC(compositionString, size);
    }

    public nint ResizeCandidateInfo(nint candidateInfo, uint size)
    {
        return Imm32Methods.ImmReSizeIMCC(candidateInfo, size);
    }

    public nint ResizeGuideLine(nint guideLine, uint size)
    {
        return Imm32Methods.ImmReSizeIMCC(guideLine, size);
    }

    public nint ResizePrivateData(nint privateData, uint size)
    {
        return Imm32Methods.ImmReSizeIMCC(privateData, size);
    }

    public bool GenerateMessage(HImc inputContext)
    {
        return Imm32Methods.ImmGenerateMessage(inputContext);
    }
}
