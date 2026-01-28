using KurbawjeleJarlayenel.SourceGenerator;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace KurbawjeleJarlayenel.OpenGL.Angle;

unsafe partial class Win32AngleEglInterface
{
    delegate* unmanaged[Stdcall]<int, nint, int*, nint> _addr_CreateDeviceANGLE;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate nint __wasmDummyCreateDeviceANGLE(int a0, nint a1, int* a2);
    public partial nint CreateDeviceANGLE(int @deviceType, nint @nativeDevice, int[]? @attribs)
    {
        if (_addr_CreateDeviceANGLE == null) throw new System.EntryPointNotFoundException("CreateDeviceANGLE");
        fixed (int* @__p_attribs = attribs)
            return _addr_CreateDeviceANGLE(@deviceType, @nativeDevice, @__p_attribs);
    }
    public bool IsCreateDeviceANGLEAvailable => _addr_CreateDeviceANGLE != null;
    delegate* unmanaged[Stdcall]<nint, void> _addr_ReleaseDeviceANGLE;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyReleaseDeviceANGLE(nint a0);
    public partial void ReleaseDeviceANGLE(nint @device)
    {
        if (_addr_ReleaseDeviceANGLE == null) throw new System.EntryPointNotFoundException("ReleaseDeviceANGLE");
        _addr_ReleaseDeviceANGLE(@device);
    }
    public bool IsReleaseDeviceANGLEAvailable => _addr_ReleaseDeviceANGLE != null;
    void Initialize(Func<string, IntPtr> getProcAddress)
    {
        var addr = IntPtr.Zero;
        // Initializing CreateDeviceANGLE
        addr = IntPtr.Zero;
        addr = getProcAddress("eglCreateDeviceANGLE");
        _addr_CreateDeviceANGLE = (delegate* unmanaged[Stdcall]<int, nint, int*, nint>) addr;
        // Initializing ReleaseDeviceANGLE
        addr = IntPtr.Zero;
        addr = getProcAddress("eglReleaseDeviceANGLE");
        _addr_ReleaseDeviceANGLE = (delegate* unmanaged[Stdcall]<nint, void>) addr;
    }
}