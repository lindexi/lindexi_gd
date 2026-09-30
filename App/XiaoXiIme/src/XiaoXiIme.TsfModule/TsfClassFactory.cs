using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace XiaoXiIme.TsfModule;

internal static unsafe class TsfClassFactory
{
    [StructLayout(LayoutKind.Sequential)]
    private struct ClassFactoryObject
    {
        public void** Vtable;
        public int ReferenceCount;
    }

    private static readonly void** s_vtable = CreateVtable();

    internal static Guid IUnknownId { get; } = new("00000000-0000-0000-C000-000000000046");

    internal static Guid IClassFactoryId { get; } = new("00000001-0000-0000-C000-000000000046");

    public static int TryCreate(Guid interfaceId, nint* result)
    {
        if (result is null)
        {
            return TsfHResults.E_POINTER;
        }

        *result = 0;
        if (interfaceId != IUnknownId && interfaceId != IClassFactoryId)
        {
            return TsfHResults.E_NOINTERFACE;
        }

        var instance = (ClassFactoryObject*) NativeMemory.AllocZeroed((nuint) sizeof(ClassFactoryObject));
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
        var vtable = (void**) NativeMemory.Alloc((nuint) (sizeof(void*) * 5));
        if (vtable is null)
        {
            Environment.FailFast("Unable to allocate the TSF class factory vtable.");
        }

        vtable[0] = (void*) (delegate* unmanaged[Stdcall]<ClassFactoryObject*, Guid*, nint*, int>) &QueryInterface;
        vtable[1] = (void*) (delegate* unmanaged[Stdcall]<ClassFactoryObject*, uint>) &AddRef;
        vtable[2] = (void*) (delegate* unmanaged[Stdcall]<ClassFactoryObject*, uint>) &Release;
        vtable[3] = (void*) (delegate* unmanaged[Stdcall]<ClassFactoryObject*, nint, Guid*, nint*, int>) &CreateInstance;
        vtable[4] = (void*) (delegate* unmanaged[Stdcall]<ClassFactoryObject*, int, int>) &LockServer;
        return vtable;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int QueryInterface(ClassFactoryObject* self, Guid* interfaceId, nint* result)
    {
        return TsfUnmanagedBoundary.Invoke(() => QueryInterfaceCore(self, interfaceId, result));
    }

    private static int QueryInterfaceCore(ClassFactoryObject* self, Guid* interfaceId, nint* result)
    {
        if (self is null || interfaceId is null || result is null)
        {
            return TsfHResults.E_POINTER;
        }

        *result = 0;
        if (*interfaceId != IUnknownId && *interfaceId != IClassFactoryId)
        {
            return TsfHResults.E_NOINTERFACE;
        }

        AddRefCore(self);
        *result = (nint) self;
        return TsfHResults.S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint AddRef(ClassFactoryObject* self)
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

    private static uint AddRefCore(ClassFactoryObject* self)
    {
        return unchecked((uint) Interlocked.Increment(ref self->ReferenceCount));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint Release(ClassFactoryObject* self)
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
    private static int CreateInstance(ClassFactoryObject* self, nint outerUnknown, Guid* interfaceId, nint* result)
    {
        return TsfUnmanagedBoundary.Invoke(() =>
        {
            if (self is null || interfaceId is null || result is null)
            {
                return TsfHResults.E_POINTER;
            }

            *result = 0;
            if (outerUnknown != 0)
            {
                return TsfHResults.CLASS_E_NOAGGREGATION;
            }

            return TsfHResults.E_NOINTERFACE;
        });
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int LockServer(ClassFactoryObject* self, int lockServer)
    {
        return TsfUnmanagedBoundary.Invoke(() =>
        {
            if (self is null)
            {
                return TsfHResults.E_POINTER;
            }

            TsfComServer.LockServer(lockServer != 0);
            return TsfHResults.S_OK;
        });
    }
}