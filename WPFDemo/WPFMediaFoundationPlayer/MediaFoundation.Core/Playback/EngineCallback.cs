using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MediaFoundation.Core;

internal sealed unsafe class EngineCallback : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Instance
    {
        internal void** Vtable;
        internal int References;
        internal nint Handle;
    }

    private Instance* _instance;
    private readonly SynchronizationContext _context;
    private readonly Action<uint, nuint, uint> _notify;
    private int _closed;
    private static readonly Guid s_notifyId = typeof(Windows.Win32.Media.MediaFoundation.IMFMediaEngineNotify).GUID;
    private static readonly void** s_vtable = CreateVtable();
    internal void* Pointer => _instance;

    internal EngineCallback(SynchronizationContext context, Action<uint, nuint, uint> notify)
    {
        this._context = context;
        this._notify = notify;
        _instance = (Instance*)NativeMemory.AllocZeroed((nuint)sizeof(Instance));
        _instance->Vtable = s_vtable;
        _instance->References = 1;
        _instance->Handle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
    }

    private static void** CreateVtable()
    {
        var table = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(EngineCallback), 4 * sizeof(void*));
        table[0] = (delegate* unmanaged[Stdcall]<Instance*, Guid*, void**, int>)&Query;
        table[1] = (delegate* unmanaged[Stdcall]<Instance*, uint>)&AddRef;
        table[2] = (delegate* unmanaged[Stdcall]<Instance*, uint>)&Release;
        table[3] = (delegate* unmanaged[Stdcall]<Instance*, uint, nuint, uint, int>)&Event;
        return table;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int Query(Instance* self, Guid* iid, void** result)
    {
        if (result == null) return unchecked((int)0x80004003);
        *result = null;
        if (iid == null) return unchecked((int)0x80004003);
        if (*iid != s_notifyId && *iid != new Guid("00000000-0000-0000-C000-000000000046"))
            return unchecked((int)0x80004002);
        Interlocked.Increment(ref self->References);
        *result = self;
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint AddRef(Instance* self) => (uint)Interlocked.Increment(ref self->References);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint Release(Instance* self) => ReleaseCore(self);

    private static uint ReleaseCore(Instance* self)
    {
        int remaining = Interlocked.Decrement(ref self->References);
        if (remaining == 0)
        {
            GCHandle.FromIntPtr(self->Handle).Free();
            NativeMemory.Free(self);
        }

        return (uint)remaining;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int Event(Instance* self, uint kind, nuint first, uint second)
    {
        try
        {
            // Media Engine waits for this acknowledgement; dispatching it to the UI can stall seek/play.
            if (kind == (uint)Windows.Win32.Media.MediaFoundation.MF_MEDIA_ENGINE_EVENT
                    .MF_MEDIA_ENGINE_EVENT_NOTIFYSTABLESTATE)
            {
                return Windows.Win32.PInvoke.SetEvent
                    (new Windows.Win32.Foundation.HANDLE((void*)first))
                    ? 0
                    : Marshal.GetHRForLastWin32Error();
            }

            var owner = (EngineCallback)GCHandle.FromIntPtr(self->Handle).Target!;
            if (Volatile.Read(ref owner._closed) == 0)
                owner._context.Post
                (
                    _ =>
                    {
                        if (Volatile.Read(ref owner._closed) == 0) owner._notify(kind, first, second);
                    }, null
                );
            return 0;
        }
        // Managed exceptions must never cross a native callback boundary.
        catch (Exception error)
        {
            Trace.TraceError("Media Engine callback dispatch: {0}", error);
            return error.HResult;
        }
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _closed, 1);
        if (_instance == null) return;
        var old = _instance;
        _instance = null;
        ReleaseCore(old);
    }
}