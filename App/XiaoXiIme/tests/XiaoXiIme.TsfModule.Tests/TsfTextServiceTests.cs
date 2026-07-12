using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using XiaoXiIme.TsfModule;

namespace XiaoXiIme.TsfModule.Tests;

public unsafe class TsfTextServiceTests
{
    [Fact]
    public void RequestEditSession_UsesSynchronousReadWriteSession()
    {
        using var context = new FakeContext(TsfHResults.S_OK, TsfHResults.S_OK);

        var hr = TsfTextService.RequestEditSession(context.Pointer, 73);

        Assert.Equal(TsfHResults.S_OK, hr);
        Assert.Equal(73u, context.ClientId);
        Assert.Equal(TsfNative.TfEditSessionSync | TsfNative.TfEditSessionReadWrite, context.Flags);
        Assert.Equal(1, context.DoEditSessionCalls);
        Assert.Equal(0, context.OutstandingSessionReferences);
    }

    [Fact]
    public void RequestEditSession_ReturnsSessionResultWhenRequestSucceeds()
    {
        using var context = new FakeContext(TsfHResults.S_OK, TsfHResults.E_UNEXPECTED);

        var hr = TsfTextService.RequestEditSession(context.Pointer, 1);

        Assert.Equal(TsfHResults.E_UNEXPECTED, hr);
        Assert.Equal(0, context.OutstandingSessionReferences);
    }

    [Fact]
    public void RequestEditSession_ReturnsRequestFailureAndReleasesSession()
    {
        using var context = new FakeContext(TsfHResults.E_NOINTERFACE, TsfHResults.S_OK);

        var hr = TsfTextService.RequestEditSession(context.Pointer, 1);

        Assert.Equal(TsfHResults.E_NOINTERFACE, hr);
        Assert.Equal(0, context.OutstandingSessionReferences);
    }

    private sealed class FakeContext : IDisposable
    {
        private readonly nint _instance;
        private readonly nint _vtable;
        private readonly int _requestResult;
        private readonly int _sessionResult;

        public FakeContext(int requestResult, int sessionResult)
        {
            _requestResult = requestResult;
            _sessionResult = sessionResult;
            _vtable = (nint) NativeMemory.AllocZeroed((nuint) (sizeof(void*) * 4));
            _instance = (nint) NativeMemory.AllocZeroed((nuint) sizeof(nint));
            *(nint*) _instance = _vtable;
            ((void**) _vtable)[TsfNative.ContextRequestEditSessionSlot] =
                (void*) (delegate* unmanaged[Stdcall]<nint, uint, nint, uint, int*, int>) &RequestEditSession;
            s_current = this;
        }

        private static FakeContext? s_current;

        public nint Pointer => _instance;

        public uint ClientId { get; private set; }

        public uint Flags { get; private set; }

        public int DoEditSessionCalls { get; private set; }

        public int OutstandingSessionReferences { get; private set; }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int RequestEditSession(nint context, uint clientId, nint editSession, uint flags, int* sessionResult)
        {
            var current = s_current!;
            current.ClientId = clientId;
            current.Flags = flags;
            current.OutstandingSessionReferences++;
            TsfComPointer.AddRef(editSession);
            try
            {
                if (current._requestResult >= 0)
                {
                    var vtable = *(void***) editSession;
                    var doEditSession = (delegate* unmanaged[Stdcall]<nint, uint, int>) vtable[3];
                    current.DoEditSessionCalls++;
                    var actualResult = doEditSession(editSession, 42);
                    *sessionResult = current._sessionResult < 0 ? current._sessionResult : actualResult;
                }

                return current._requestResult;
            }
            finally
            {
                TsfComPointer.Release(editSession);
                current.OutstandingSessionReferences--;
            }
        }

        public void Dispose()
        {
            s_current = null;
            NativeMemory.Free((void*) _instance);
            NativeMemory.Free((void*) _vtable);
        }
    }
}
