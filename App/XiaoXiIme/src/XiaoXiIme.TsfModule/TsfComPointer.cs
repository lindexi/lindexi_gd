namespace XiaoXiIme.TsfModule;

internal static unsafe class TsfComPointer
{
    internal static int QueryInterface(nint instance, Guid interfaceId, nint* result)
    {
        if (instance == 0 || result is null)
        {
            return TsfHResults.E_POINTER;
        }

        *result = 0;
        var vtable = *(void***) instance;
        var queryInterface = (delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>) vtable[TsfNative.IUnknownQueryInterfaceSlot];
        return queryInterface(instance, &interfaceId, result);
    }

    internal static uint AddRef(nint instance)
    {
        if (instance == 0)
        {
            return 0;
        }

        var vtable = *(void***) instance;
        var addRef = (delegate* unmanaged[Stdcall]<nint, uint>) vtable[TsfNative.IUnknownAddRefSlot];
        return addRef(instance);
    }

    internal static uint Release(nint instance)
    {
        if (instance == 0)
        {
            return 0;
        }

        var vtable = *(void***) instance;
        var release = (delegate* unmanaged[Stdcall]<nint, uint>) vtable[TsfNative.IUnknownReleaseSlot];
        return release(instance);
    }
}