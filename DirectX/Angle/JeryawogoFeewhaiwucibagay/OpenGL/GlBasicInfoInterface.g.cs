using System;

namespace JeryawogoFeewhaiwucibagay.OpenGL;

unsafe partial class GlBasicInfoInterface
{
    delegate* unmanaged[Stdcall]<int, int*, void> _addr_GetIntegerv;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyGetIntegerv(int a0, int* a1);
    public partial void GetIntegerv(int @name, out int @rv)
    {
        fixed (int* @__p_rv = &rv)
            _addr_GetIntegerv(@name, @__p_rv);
    }
    delegate* unmanaged[Stdcall]<int, float*, void> _addr_GetFloatv;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyGetFloatv(int a0, float* a1);
    public partial void GetFloatv(int @name, out float @rv)
    {
        fixed (float* @__p_rv = &rv)
            _addr_GetFloatv(@name, @__p_rv);
    }
    delegate* unmanaged[Stdcall]<int, nint> _addr_GetStringNative;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate nint __wasmDummyGetStringNative(int a0);
    public partial nint GetStringNative(int @v)
    {
        return _addr_GetStringNative(@v);
    }
    delegate* unmanaged[Stdcall]<int, int, nint> _addr_GetStringiNative;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate nint __wasmDummyGetStringiNative(int a0, int a1);
    public partial nint GetStringiNative(int @v, int @v1)
    {
        return _addr_GetStringiNative(@v, @v1);
    }
    delegate* unmanaged[Stdcall]<int> _addr_GetError;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyGetError();
    public partial int GetError()
    {
        return _addr_GetError();
    }
    void Initialize(Func<string, IntPtr> getProcAddress)
    {
        var addr = IntPtr.Zero;
        // Initializing GetIntegerv
        addr = IntPtr.Zero;
        addr = getProcAddress("glGetIntegerv");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetIntegerv");
        _addr_GetIntegerv = (delegate* unmanaged[Stdcall]<int, int*, void>) addr;
        // Initializing GetFloatv
        addr = IntPtr.Zero;
        addr = getProcAddress("glGetFloatv");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetFloatv");
        _addr_GetFloatv = (delegate* unmanaged[Stdcall]<int, float*, void>) addr;
        // Initializing GetStringNative
        addr = IntPtr.Zero;
        addr = getProcAddress("glGetString");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetStringNative");
        _addr_GetStringNative = (delegate* unmanaged[Stdcall]<int, nint>) addr;
        // Initializing GetStringiNative
        addr = IntPtr.Zero;
        addr = getProcAddress("glGetStringi");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetStringiNative");
        _addr_GetStringiNative = (delegate* unmanaged[Stdcall]<int, int, nint>) addr;
        // Initializing GetError
        addr = IntPtr.Zero;
        addr = getProcAddress("glGetError");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetError");
        _addr_GetError = (delegate* unmanaged[Stdcall]<int>) addr;
    }
}