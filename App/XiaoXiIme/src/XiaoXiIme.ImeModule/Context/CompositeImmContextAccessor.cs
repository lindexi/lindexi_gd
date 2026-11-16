using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

internal sealed class CompositeImmContextAccessor : IImmContextAccessor
{
    private readonly InMemoryImmContext[] _contexts;

    public CompositeImmContextAccessor(params InMemoryImmContext[] contexts)
    {
        ArgumentNullException.ThrowIfNull(contexts);
        _contexts = contexts;
    }

    public ImeCompositionContextWriter CreateWriter() => new(this);

    public ImeCompositionContextReader CreateReader() => new(this);

    public nint LockInputContext(HImc inputContext) => FirstNonZero(context => context.LockInputContext(inputContext));

    public bool UnlockInputContext(HImc inputContext) => UnlockAll(context => context.UnlockInputContext(inputContext));

    public nint LockCompositionString(nint compositionString) => FirstNonZero
        (context => context.LockCompositionString(compositionString));

    public bool UnlockCompositionString(nint compositionString) => UnlockAll
        (context => context.UnlockCompositionString(compositionString));

    public nint LockCandidateInfo(nint candidateInfo) => FirstNonZero
        (context => context.LockCandidateInfo(candidateInfo));

    public bool UnlockCandidateInfo(nint candidateInfo) => UnlockAll
        (context => context.UnlockCandidateInfo(candidateInfo));

    public nint LockGuideLine(nint guideLine) => FirstNonZero(context => context.LockGuideLine(guideLine));

    public bool UnlockGuideLine(nint guideLine) => UnlockAll(context => context.UnlockGuideLine(guideLine));

    public nint LockPrivateData(nint privateData) => FirstNonZero(context => context.LockPrivateData(privateData));

    public bool UnlockPrivateData(nint privateData) => UnlockAll(context => context.UnlockPrivateData(privateData));

    public nint ResizeCompositionString(nint compositionString, uint size) =>
        FirstNonZero(context => context.ResizeCompositionString(compositionString, size));

    public nint ResizeCandidateInfo(nint candidateInfo, uint size) =>
        FirstNonZero(context => context.ResizeCandidateInfo(candidateInfo, size));

    public nint ResizeGuideLine(nint guideLine, uint size) =>
        FirstNonZero(context => context.ResizeGuideLine(guideLine, size));

    public nint ResizePrivateData(nint privateData, uint size) =>
        FirstNonZero(context => context.ResizePrivateData(privateData, size));

    public bool GenerateMessage(HImc inputContext) => UnlockAll(context => context.GenerateMessage(inputContext));

    private nint FirstNonZero(Func<InMemoryImmContext, nint> lockHandle)
    {
        foreach (var context in _contexts)
        {
            var pointer = lockHandle(context);
            if (pointer != 0)
            {
                return pointer;
            }
        }

        return 0;
    }

    private bool UnlockAll(Func<InMemoryImmContext, bool> unlock)
    {
        var succeeded = true;
        foreach (var context in _contexts)
        {
            succeeded &= unlock(context);
        }

        return succeeded;
    }
}