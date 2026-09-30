using System.Runtime.InteropServices;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

internal sealed unsafe class InMemoryImmContext : IImmContextAccessor, IDisposable
{
    public const nint DefaultHandle = 1;
    public const nint CompositionHandle = 2;
    public const nint CandidateHandle = 3;
    public const nint GuideLineHandle = 4;
    public const nint PrivateHandle = 5;
    private const nint HandleStride = 16;

    private static int s_nextHandleOffset;

    private readonly byte* _inputContext;
    private readonly byte* _compositionBuffer;
    private readonly byte* _candidateBuffer;
    private readonly byte* _guideLineBuffer;
    private readonly byte* _privateBuffer;
    private readonly nint _handle;
    private bool _disposed;

    public InMemoryImmContext(
        int compositionBufferSize = 2048,
        int candidateBufferSize = 65536,
        int guideLineBufferSize = 2048,
        int privateBufferSize = 256)
        : this(AllocateHandle(), compositionBufferSize, candidateBufferSize, guideLineBufferSize, privateBufferSize)
    {
    }

    public InMemoryImmContext(
        nint handle,
        int compositionBufferSize = 2048,
        int candidateBufferSize = 65536,
        int guideLineBufferSize = 2048,
        int privateBufferSize = 256)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(handle, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(compositionBufferSize, sizeof(CompositionString));
        ArgumentOutOfRangeException.ThrowIfLessThan(candidateBufferSize, sizeof(CandidateInfo));
        ArgumentOutOfRangeException.ThrowIfLessThan(guideLineBufferSize, sizeof(GuideLine));
        ArgumentOutOfRangeException.ThrowIfLessThan(privateBufferSize, sizeof(ImePrivateData));

        _inputContext = (byte*)NativeMemory.AllocZeroed((nuint)sizeof(InputContext));
        _compositionBuffer = (byte*)NativeMemory.AllocZeroed((nuint)compositionBufferSize);
        _candidateBuffer = (byte*)NativeMemory.AllocZeroed((nuint)candidateBufferSize);
        _guideLineBuffer = (byte*)NativeMemory.AllocZeroed((nuint)guideLineBufferSize);
        _privateBuffer = (byte*)NativeMemory.AllocZeroed((nuint)privateBufferSize);

        var context = (InputContext*)_inputContext;
        _handle = handle;
        context->HCompStr = handle + (CompositionHandle - DefaultHandle);
        context->HCandInfo = handle + (CandidateHandle - DefaultHandle);
        context->HGuideLine = handle + (GuideLineHandle - DefaultHandle);
        context->HPrivate = handle + (PrivateHandle - DefaultHandle);
    }

    public bool FailResize { get; init; }

    public nint Handle => _handle;

    public HImc HImc => new(_handle);

    public InputContext* InputContextPointer
    {
        get
        {
            ThrowIfDisposed();
            return (InputContext*)_inputContext;
        }
    }

    public CompositionString* CompositionString
    {
        get
        {
            ThrowIfDisposed();
            return (CompositionString*)_compositionBuffer;
        }
    }

    public CandidateInfo* CandidateInfo
    {
        get
        {
            ThrowIfDisposed();
            return (CandidateInfo*)_candidateBuffer;
        }
    }

    public ImeCompositionContextWriter CreateWriter() => new(this);

    public ImeCompositionContextReader CreateReader() => new(this);

    public string ReadCompositionText()
    {
        ThrowIfDisposed();
        return ReadString(_compositionBuffer, CompositionString->CompStrOffset, CompositionString->CompStrLength);
    }

    public string ReadResultText()
    {
        ThrowIfDisposed();
        return ReadString(_compositionBuffer, CompositionString->ResultStrOffset, CompositionString->ResultStrLength);
    }

    public string ReadFirstCandidateText()
    {
        ThrowIfDisposed();
        var candidateInfo = CandidateInfo;
        if (candidateInfo->Count == 0)
        {
            return string.Empty;
        }

        var candidateList = (CandidateList*)(_candidateBuffer + candidateInfo->Offset[0]);
        return new string((char*)((byte*)candidateList + candidateList->Offset[0]));
    }

    public nint LockInputContext(HImc inputContext)
    {
        ThrowIfDisposed();
        return inputContext.Value == _handle ? (nint)_inputContext : 0;
    }

    public bool UnlockInputContext(HImc inputContext) => true;

    public nint LockCompositionString(nint compositionString)
    {
        ThrowIfDisposed();
        return compositionString == ContextHandle(CompositionHandle) ? (nint)_compositionBuffer : 0;
    }

    public bool UnlockCompositionString(nint compositionString) => true;

    public nint LockCandidateInfo(nint candidateInfo)
    {
        ThrowIfDisposed();
        return candidateInfo == ContextHandle(CandidateHandle) ? (nint)_candidateBuffer : 0;
    }

    public bool UnlockCandidateInfo(nint candidateInfo) => true;

    public nint LockGuideLine(nint guideLine)
    {
        ThrowIfDisposed();
        return guideLine == ContextHandle(GuideLineHandle) ? (nint)_guideLineBuffer : 0;
    }

    public bool UnlockGuideLine(nint guideLine) => true;

    public nint LockPrivateData(nint privateData)
    {
        ThrowIfDisposed();
        return privateData == ContextHandle(PrivateHandle) ? (nint)_privateBuffer : 0;
    }

    public bool UnlockPrivateData(nint privateData) => true;

    public nint ResizeCompositionString(nint compositionString, uint size) => FailResize ? 0 : compositionString;

    public nint ResizeCandidateInfo(nint candidateInfo, uint size) => FailResize ? 0 : candidateInfo;

    public nint ResizeGuideLine(nint guideLine, uint size) => FailResize ? 0 : guideLine;

    public nint ResizePrivateData(nint privateData, uint size) => FailResize ? 0 : privateData;

    public bool GenerateMessage(HImc inputContext) => true;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        NativeMemory.Free(_inputContext);
        NativeMemory.Free(_compositionBuffer);
        NativeMemory.Free(_candidateBuffer);
        NativeMemory.Free(_guideLineBuffer);
        NativeMemory.Free(_privateBuffer);
        _disposed = true;
    }

    private static string ReadString(byte* buffer, uint offset, uint length)
    {
        if (length == 0)
        {
            return string.Empty;
        }

        return new string((char*)(buffer + offset), 0, (int)(length / sizeof(char)));
    }

    private nint ContextHandle(nint baseHandle) => _handle + (baseHandle - DefaultHandle);

    private static nint AllocateHandle()
    {
        return DefaultHandle + (Interlocked.Increment(ref s_nextHandleOffset) * HandleStride);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}

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

    public nint LockCompositionString(nint compositionString) => FirstNonZero(context => context.LockCompositionString(compositionString));

    public bool UnlockCompositionString(nint compositionString) => UnlockAll(context => context.UnlockCompositionString(compositionString));

    public nint LockCandidateInfo(nint candidateInfo) => FirstNonZero(context => context.LockCandidateInfo(candidateInfo));

    public bool UnlockCandidateInfo(nint candidateInfo) => UnlockAll(context => context.UnlockCandidateInfo(candidateInfo));

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
