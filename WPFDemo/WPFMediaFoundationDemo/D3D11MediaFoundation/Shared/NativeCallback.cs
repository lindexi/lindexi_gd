using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Media.MediaFoundation;

namespace MediaSamples;

internal unsafe sealed class NativeCallback : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Instance
    {
        internal void** VTable;
        internal nint Handle;
        internal int References;
    }

    private Instance* _instance;
    private readonly Guid _iid;
    internal Action<uint, nuint, uint>? Notify;
    internal Action<nint>? Invoke;
    internal Exception? Failure;
    internal void* Pointer => _instance;

    internal NativeCallback(Guid iid, bool mediaEngine = false)
    {
        _iid = iid;
        _instance = (Instance*)NativeMemory.AllocZeroed((nuint)sizeof(Instance));
        _instance->Handle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
        _instance->References = 1;
        _instance->VTable = (void**)NativeMemory.Alloc((nuint)(4 * sizeof(void*)));
        _instance->VTable[0] = (delegate* unmanaged[Stdcall]<Instance*, Guid*, void**, int>)&QueryInterface;
        _instance->VTable[1] = (delegate* unmanaged[Stdcall]<Instance*, uint>)&AddRef;
        _instance->VTable[2] = (delegate* unmanaged[Stdcall]<Instance*, uint>)&Release;
        _instance->VTable[3] = mediaEngine
            ? (void*)(delegate* unmanaged[Stdcall]<Instance*, uint, nuint, uint, int>)&EventNotify
            : (void*)(delegate* unmanaged[Stdcall]<Instance*, void*, int>)&OnObject;
    }

    private static NativeCallback Owner(Instance* self) => (NativeCallback)GCHandle.FromIntPtr(self->Handle).Target!;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int QueryInterface(Instance* self, Guid* iid, void** result)
    {
        if (result == null) return unchecked((int)0x80004003);
        *result = null;
        if (*iid != Owner(self)._iid && *iid != new Guid("00000000-0000-0000-C000-000000000046")) return unchecked((int)0x80004002);
        *result = self;
        Interlocked.Increment(ref self->References);
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static uint AddRef(Instance* self) => (uint)Interlocked.Increment(ref self->References);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static uint Release(Instance* self) => ReleaseCore(self);

    private static uint ReleaseCore(Instance* self)
    {
        int remaining = Interlocked.Decrement(ref self->References);
        if (remaining == 0)
        {
            GCHandle.FromIntPtr(self->Handle).Free();
            NativeMemory.Free(self->VTable);
            NativeMemory.Free(self);
        }
        return (uint)remaining;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int EventNotify(Instance* self, uint kind, nuint first, uint second)
    {
        var owner = Owner(self);
        try { owner.Notify?.Invoke(kind, first, second); return 0; }
        catch (Exception error) { owner.Failure = error; return error.HResult; }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int OnObject(Instance* self, void* value)
    {
        var owner = Owner(self);
        try { owner.Invoke?.Invoke((nint)value); return 0; }
        catch (Exception error) { owner.Failure = error; return error.HResult; }
    }

    public void Dispose()
    {
        if (_instance == null) return;
        var instance = _instance;
        _instance = null;
        ReleaseCore(instance);
    }
}
