using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace XiaoXiIme.TsfModule;

internal static unsafe class TsfEditSession
{
    [StructLayout(LayoutKind.Sequential)]
    private struct EditSessionObject
    {
        public void** Vtable;
        public int ReferenceCount;
    }

    private static readonly void** s_vtable = CreateVtable();

    internal static int TryCreate(nint* result)
    {
        if (result is null)
        {
            return TsfHResults.E_POINTER;
        }

        *result = 0;
        var instance = (EditSessionObject*) NativeMemory.AllocZeroed((nuint) sizeof(EditSessionObject));
        if (instance is null)
        {
            return TsfHResults.E_OUTOFMEMORY;
        }

        instance->Vtable = s_vtable;
        instance->ReferenceCount = 1;
        TsfComServer.AddObject();
        *result = (nint) instance;
        return TsfHResults.S_OK;
    }

    private static void** CreateVtable()
    {
        var vtable = (void**) NativeMemory.Alloc((nuint) (sizeof(void*) * 4));
        if (vtable is null)
        {
            Environment.FailFast("Unable to allocate the TSF edit session vtable.");
        }

        vtable[0] = (void*) (delegate* unmanaged[Stdcall]<EditSessionObject*, Guid*, nint*, int>) &QueryInterface;
        vtable[1] = (void*) (delegate* unmanaged[Stdcall]<EditSessionObject*, uint>) &AddRef;
        vtable[2] = (void*) (delegate* unmanaged[Stdcall]<EditSessionObject*, uint>) &Release;
        vtable[3] = (void*) (delegate* unmanaged[Stdcall]<EditSessionObject*, uint, int>) &DoEditSession;
        return vtable;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int QueryInterface(EditSessionObject* self, Guid* interfaceId, nint* result) =>
        TsfUnmanagedBoundary.Invoke(() =>
        {
            if (self is null || interfaceId is null || result is null)
            {
                return TsfHResults.E_POINTER;
            }

            *result = 0;
            if (*interfaceId != TsfNative.IUnknownId && *interfaceId != TsfNative.IEditSessionId)
            {
                return TsfHResults.E_NOINTERFACE;
            }

            AddRefCore(self);
            *result = (nint) self;
            return TsfHResults.S_OK;
        });

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint AddRef(EditSessionObject* self)
    {
        try
        {
            return self is null ? 0 : AddRefCore(self);
        }
        catch
        {
            return 0;
        }
    }

    private static uint AddRefCore(EditSessionObject* self) =>
        unchecked((uint) Interlocked.Increment(ref self->ReferenceCount));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint Release(EditSessionObject* self)
    {
        try
        {
            if (self is null)
            {
                return 0;
            }

            var referenceCount = Interlocked.Decrement(ref self->ReferenceCount);
            if (referenceCount == 0)
            {
                NativeMemory.Free(self);
                TsfComServer.ReleaseObject();
            }

            return referenceCount < 0 ? 0 : unchecked((uint) referenceCount);
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int DoEditSession(EditSessionObject* self, uint editCookie) =>
        TsfUnmanagedBoundary.Invoke(() => self is null ? TsfHResults.E_POINTER : TsfHResults.S_OK);
}
