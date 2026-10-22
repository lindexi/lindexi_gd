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

    private Instance* instance;
    private readonly SynchronizationContext context;
    private readonly Action<uint, uint> notify;
    private int closed;
    private static readonly Guid NotifyId = new("FEE7C112-E776-42B5-9BBF-0048524E2BD5");
    private static readonly void** Vtable = CreateVtable();
    internal void* Pointer => instance;

    internal EngineCallback(SynchronizationContext context, Action<uint, uint> notify)
    {
        this.context = context;
        this.notify = notify;
        instance = (Instance*)NativeMemory.AllocZeroed((nuint)sizeof(Instance));
        instance->Vtable = Vtable;
        instance->References = 1;
        instance->Handle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
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
        if (*iid != NotifyId && *iid != new Guid("00000000-0000-0000-C000-000000000046")) return unchecked((int)0x80004002);
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
            var owner = (EngineCallback)GCHandle.FromIntPtr(self->Handle).Target!;
            if (Volatile.Read(ref owner.closed) == 0)
                owner.context.Post(_ => { if (Volatile.Read(ref owner.closed) == 0) owner.notify(kind, second); }, null);
            return 0;
        }
        // Managed exceptions must never cross a native callback boundary.
        catch (Exception error) { Trace.TraceError("Media Engine callback dispatch: {0}", error); return error.HResult; }
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref closed, 1);
        if (instance == null) return;
        var old = instance;
        instance = null;
        ReleaseCore(old);
    }
}
