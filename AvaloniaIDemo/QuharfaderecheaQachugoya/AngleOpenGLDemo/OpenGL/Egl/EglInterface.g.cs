using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace JeryawogoFeewhaiwucibagay.OpenGL.Egl;

unsafe partial class EglInterface
{
    delegate* unmanaged[Stdcall]<int> _addr_GetError;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyGetError();
    public partial int GetError()
    {
        return _addr_GetError();
    }
    delegate* unmanaged[Stdcall]<nint, nint> _addr_GetDisplay;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate nint __wasmDummyGetDisplay(nint a0);
    public partial nint GetDisplay(nint @nativeDisplay)
    {
        return _addr_GetDisplay(@nativeDisplay);
    }
    delegate* unmanaged[Stdcall]<int, nint, int*, nint> _addr_GetPlatformDisplayExt;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate nint __wasmDummyGetPlatformDisplayExt(int a0, nint a1, int* a2);
    public partial nint GetPlatformDisplayExt(int @platform, nint @nativeDisplay, int[]? @attrs)
    {
        if (_addr_GetPlatformDisplayExt == null) throw new System.EntryPointNotFoundException("GetPlatformDisplayExt");
        fixed (int* @__p_attrs = attrs)
            return _addr_GetPlatformDisplayExt(@platform, @nativeDisplay, @__p_attrs);
    }
    public bool IsGetPlatformDisplayExtAvailable => _addr_GetPlatformDisplayExt != null;
    delegate* unmanaged[Stdcall]<nint, int*, int*, int> _addr_Initialize;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyInitialize(nint a0, int* a1, int* a2);
    public partial bool Initialize(nint @display, out int @major, out int @minor)
    {
        fixed (int* @__p_major = &major)
        fixed (int* @__p_minor = &minor)
            return _addr_Initialize(@display, @__p_major, @__p_minor) != 0;
    }
    delegate* unmanaged[Stdcall]<nint, void> _addr_Terminate;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyTerminate(nint a0);
    public partial void Terminate(nint @display)
    {
        _addr_Terminate(@display);
    }
    delegate* unmanaged[Stdcall]<nint, nint> _addr_GetProcAddress;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate nint __wasmDummyGetProcAddress(nint a0);
    public partial nint GetProcAddress(nint @proc)
    {
        return _addr_GetProcAddress(@proc);
    }
    delegate* unmanaged[Stdcall]<int, int> _addr_BindApi;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyBindApi(int a0);
    public partial bool BindApi(int @api)
    {
        return _addr_BindApi(@api) != 0;
    }
    delegate* unmanaged[Stdcall]<nint, int*, nint*, int, int*, int> _addr_ChooseConfig;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyChooseConfig(nint a0, int* a1, nint* a2, int a3, int* a4);
    public partial bool ChooseConfig(nint @display, int[] @attribs, out nint @surfaceConfig, int @numConfigs, out int @choosenConfig)
    {
        fixed (int* @__p_attribs = attribs)
        fixed (nint* @__p_surfaceConfig = &surfaceConfig)
        fixed (int* @__p_choosenConfig = &choosenConfig)
            return _addr_ChooseConfig(@display, @__p_attribs, @__p_surfaceConfig, @numConfigs, @__p_choosenConfig) != 0;
    }
    delegate* unmanaged[Stdcall]<nint, nint, nint, int*, nint> _addr_CreateContext;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate nint __wasmDummyCreateContext(nint a0, nint a1, nint a2, int* a3);
    public partial nint CreateContext(nint @display, nint @config, nint @share, int[] @attrs)
    {
        fixed (int* @__p_attrs = attrs)
            return _addr_CreateContext(@display, @config, @share, @__p_attrs);
    }
    delegate* unmanaged[Stdcall]<nint, nint, int> _addr_DestroyContext;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyDestroyContext(nint a0, nint a1);
    public partial bool DestroyContext(nint @display, nint @context)
    {
        return _addr_DestroyContext(@display, @context) != 0;
    }
    delegate* unmanaged[Stdcall]<nint, nint, int*, nint> _addr_CreatePBufferSurface;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate nint __wasmDummyCreatePBufferSurface(nint a0, nint a1, int* a2);
    public partial nint CreatePBufferSurface(nint @display, nint @config, int[]? @attrs)
    {
        fixed (int* @__p_attrs = attrs)
            return _addr_CreatePBufferSurface(@display, @config, @__p_attrs);
    }
    delegate* unmanaged[Stdcall]<nint, nint, nint, nint, int> _addr_MakeCurrent;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyMakeCurrent(nint a0, nint a1, nint a2, nint a3);
    public partial bool MakeCurrent(nint @display, nint @draw, nint @read, nint @context)
    {
        return _addr_MakeCurrent(@display, @draw, @read, @context) != 0;
    }
    delegate* unmanaged[Stdcall]<nint> _addr_GetCurrentContext;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate nint __wasmDummyGetCurrentContext();
    public partial nint GetCurrentContext()
    {
        return _addr_GetCurrentContext();
    }
    delegate* unmanaged[Stdcall]<nint> _addr_GetCurrentDisplay;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate nint __wasmDummyGetCurrentDisplay();
    public partial nint GetCurrentDisplay()
    {
        return _addr_GetCurrentDisplay();
    }
    delegate* unmanaged[Stdcall]<int, nint> _addr_GetCurrentSurface;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate nint __wasmDummyGetCurrentSurface(int a0);
    public partial nint GetCurrentSurface(int @readDraw)
    {
        return _addr_GetCurrentSurface(@readDraw);
    }
    delegate* unmanaged[Stdcall]<nint, nint, void> _addr_DestroySurface;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyDestroySurface(nint a0, nint a1);
    public partial void DestroySurface(nint @display, nint @surface)
    {
        _addr_DestroySurface(@display, @surface);
    }
    delegate* unmanaged[Stdcall]<nint, nint, void> _addr_SwapBuffers;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummySwapBuffers(nint a0, nint a1);
    public partial void SwapBuffers(nint @display, nint @surface)
    {
        _addr_SwapBuffers(@display, @surface);
    }
    delegate* unmanaged[Stdcall]<nint, nint, nint, int*, nint> _addr_CreateWindowSurface;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate nint __wasmDummyCreateWindowSurface(nint a0, nint a1, nint a2, int* a3);
    public partial nint CreateWindowSurface(nint @display, nint @config, nint @window, int[]? @attrs)
    {
        fixed (int* @__p_attrs = attrs)
            return _addr_CreateWindowSurface(@display, @config, @window, @__p_attrs);
    }
    delegate* unmanaged[Stdcall]<nint, nint, int, int> _addr_BindTexImage;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyBindTexImage(nint a0, nint a1, int a2);
    public partial int BindTexImage(nint @display, nint @surface, int @buffer)
    {
        return _addr_BindTexImage(@display, @surface, @buffer);
    }
    delegate* unmanaged[Stdcall]<nint, nint, int, int*, int> _addr_GetConfigAttrib;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyGetConfigAttrib(nint a0, nint a1, int a2, int* a3);
    public partial bool GetConfigAttrib(nint @display, nint @config, int @attr, out int @rv)
    {
        fixed (int* @__p_rv = &rv)
            return _addr_GetConfigAttrib(@display, @config, @attr, @__p_rv) != 0;
    }
    delegate* unmanaged[Stdcall]<int> _addr_WaitGL;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyWaitGL();
    public partial bool WaitGL()
    {
        return _addr_WaitGL() != 0;
    }
    delegate* unmanaged[Stdcall]<int> _addr_WaitClient;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyWaitClient();
    public partial bool WaitClient()
    {
        return _addr_WaitClient() != 0;
    }
    delegate* unmanaged[Stdcall]<int, int> _addr_WaitNative;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyWaitNative(int a0);
    public partial bool WaitNative(int @engine)
    {
        return _addr_WaitNative(@engine) != 0;
    }
    delegate* unmanaged[Stdcall]<nint, int, nint> _addr_QueryStringNative;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate nint __wasmDummyQueryStringNative(nint a0, int a1);
    public partial nint QueryStringNative(nint @display, int @i)
    {
        return _addr_QueryStringNative(@display, @i);
    }
    delegate* unmanaged[Stdcall]<nint, int, nint, nint, int*, nint> _addr_CreatePbufferFromClientBuffer;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate nint __wasmDummyCreatePbufferFromClientBuffer(nint a0, int a1, nint a2, nint a3, int* a4);
    public partial nint CreatePbufferFromClientBuffer(nint @display, int @buftype, nint @buffer, nint @config, int[]? @attrib_list)
    {
        fixed (int* @__p_attrib_list = attrib_list)
            return _addr_CreatePbufferFromClientBuffer(@display, @buftype, @buffer, @config, @__p_attrib_list);
    }
    delegate* unmanaged[Stdcall]<nint, int, nint, nint, int*, nint> _addr_CreatePbufferFromClientBufferPtr;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate nint __wasmDummyCreatePbufferFromClientBufferPtr(nint a0, int a1, nint a2, nint a3, int* a4);
    public partial nint CreatePbufferFromClientBufferPtr(nint @display, int @buftype, nint @buffer, nint @config, int* @attrib_list)
    {
        return _addr_CreatePbufferFromClientBufferPtr(@display, @buftype, @buffer, @config, @attrib_list);
    }
    delegate* unmanaged[Stdcall]<nint, int, nint*, int> _addr_QueryDisplayAttribExt;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyQueryDisplayAttribExt(nint a0, int a1, nint* a2);
    public partial bool QueryDisplayAttribExt(nint @display, int @attr, out nint @res)
    {
        if (_addr_QueryDisplayAttribExt == null) throw new System.EntryPointNotFoundException("QueryDisplayAttribExt");
        fixed (nint* @__p_res = &res)
            return _addr_QueryDisplayAttribExt(@display, @attr, @__p_res) != 0;
    }
    public bool IsQueryDisplayAttribExtAvailable => _addr_QueryDisplayAttribExt != null;
    delegate* unmanaged[Stdcall]<nint, int, nint*, int> _addr_QueryDeviceAttribExt;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyQueryDeviceAttribExt(nint a0, int a1, nint* a2);
    public partial bool QueryDeviceAttribExt(nint @display, int @attr, out nint @res)
    {
        if (_addr_QueryDeviceAttribExt == null) throw new System.EntryPointNotFoundException("QueryDeviceAttribExt");
        fixed (nint* @__p_res = &res)
            return _addr_QueryDeviceAttribExt(@display, @attr, @__p_res) != 0;
    }
    public bool IsQueryDeviceAttribExtAvailable => _addr_QueryDeviceAttribExt != null;
    void Initialize(Func<string, IntPtr> getProcAddress)
    {
        var addr = IntPtr.Zero;
        // Initializing GetError
        addr = IntPtr.Zero;
        addr = getProcAddress("eglGetError");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetError");
        _addr_GetError = (delegate* unmanaged[Stdcall]<int>) addr;
        // Initializing GetDisplay
        addr = IntPtr.Zero;
        addr = getProcAddress("eglGetDisplay");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetDisplay");
        _addr_GetDisplay = (delegate* unmanaged[Stdcall]<nint, nint>) addr;
        // Initializing GetPlatformDisplayExt
        addr = IntPtr.Zero;
        addr = getProcAddress("eglGetPlatformDisplayEXT");
        _addr_GetPlatformDisplayExt = (delegate* unmanaged[Stdcall]<int, nint, int*, nint>) addr;
        // Initializing Initialize
        addr = IntPtr.Zero;
        addr = getProcAddress("eglInitialize");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_Initialize");
        _addr_Initialize = (delegate* unmanaged[Stdcall]<nint, int*, int*, int>) addr;
        // Initializing Terminate
        addr = IntPtr.Zero;
        addr = getProcAddress("eglTerminate");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_Terminate");
        _addr_Terminate = (delegate* unmanaged[Stdcall]<nint, void>) addr;
        // Initializing GetProcAddress
        addr = IntPtr.Zero;
        addr = getProcAddress("eglGetProcAddress");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetProcAddress");
        _addr_GetProcAddress = (delegate* unmanaged[Stdcall]<nint, nint>) addr;
        // Initializing BindApi
        addr = IntPtr.Zero;
        addr = getProcAddress("eglBindAPI");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_BindApi");
        _addr_BindApi = (delegate* unmanaged[Stdcall]<int, int>) addr;
        // Initializing ChooseConfig
        addr = IntPtr.Zero;
        addr = getProcAddress("eglChooseConfig");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_ChooseConfig");
        _addr_ChooseConfig = (delegate* unmanaged[Stdcall]<nint, int*, nint*, int, int*, int>) addr;
        // Initializing CreateContext
        addr = IntPtr.Zero;
        addr = getProcAddress("eglCreateContext");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_CreateContext");
        _addr_CreateContext = (delegate* unmanaged[Stdcall]<nint, nint, nint, int*, nint>) addr;
        // Initializing DestroyContext
        addr = IntPtr.Zero;
        addr = getProcAddress("eglDestroyContext");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_DestroyContext");
        _addr_DestroyContext = (delegate* unmanaged[Stdcall]<nint, nint, int>) addr;
        // Initializing CreatePBufferSurface
        addr = IntPtr.Zero;
        addr = getProcAddress("eglCreatePbufferSurface");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_CreatePBufferSurface");
        _addr_CreatePBufferSurface = (delegate* unmanaged[Stdcall]<nint, nint, int*, nint>) addr;
        // Initializing MakeCurrent
        addr = IntPtr.Zero;
        addr = getProcAddress("eglMakeCurrent");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_MakeCurrent");
        _addr_MakeCurrent = (delegate* unmanaged[Stdcall]<nint, nint, nint, nint, int>) addr;
        // Initializing GetCurrentContext
        addr = IntPtr.Zero;
        addr = getProcAddress("eglGetCurrentContext");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetCurrentContext");
        _addr_GetCurrentContext = (delegate* unmanaged[Stdcall]<nint>) addr;
        // Initializing GetCurrentDisplay
        addr = IntPtr.Zero;
        addr = getProcAddress("eglGetCurrentDisplay");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetCurrentDisplay");
        _addr_GetCurrentDisplay = (delegate* unmanaged[Stdcall]<nint>) addr;
        // Initializing GetCurrentSurface
        addr = IntPtr.Zero;
        addr = getProcAddress("eglGetCurrentSurface");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetCurrentSurface");
        _addr_GetCurrentSurface = (delegate* unmanaged[Stdcall]<int, nint>) addr;
        // Initializing DestroySurface
        addr = IntPtr.Zero;
        addr = getProcAddress("eglDestroySurface");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_DestroySurface");
        _addr_DestroySurface = (delegate* unmanaged[Stdcall]<nint, nint, void>) addr;
        // Initializing SwapBuffers
        addr = IntPtr.Zero;
        addr = getProcAddress("eglSwapBuffers");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_SwapBuffers");
        _addr_SwapBuffers = (delegate* unmanaged[Stdcall]<nint, nint, void>) addr;
        // Initializing CreateWindowSurface
        addr = IntPtr.Zero;
        addr = getProcAddress("eglCreateWindowSurface");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_CreateWindowSurface");
        _addr_CreateWindowSurface = (delegate* unmanaged[Stdcall]<nint, nint, nint, int*, nint>) addr;
        // Initializing BindTexImage
        addr = IntPtr.Zero;
        addr = getProcAddress("eglBindTexImage");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_BindTexImage");
        _addr_BindTexImage = (delegate* unmanaged[Stdcall]<nint, nint, int, int>) addr;
        // Initializing GetConfigAttrib
        addr = IntPtr.Zero;
        addr = getProcAddress("eglGetConfigAttrib");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetConfigAttrib");
        _addr_GetConfigAttrib = (delegate* unmanaged[Stdcall]<nint, nint, int, int*, int>) addr;
        // Initializing WaitGL
        addr = IntPtr.Zero;
        addr = getProcAddress("eglWaitGL");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_WaitGL");
        _addr_WaitGL = (delegate* unmanaged[Stdcall]<int>) addr;
        // Initializing WaitClient
        addr = IntPtr.Zero;
        addr = getProcAddress("eglWaitClient");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_WaitClient");
        _addr_WaitClient = (delegate* unmanaged[Stdcall]<int>) addr;
        // Initializing WaitNative
        addr = IntPtr.Zero;
        addr = getProcAddress("eglWaitNative");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_WaitNative");
        _addr_WaitNative = (delegate* unmanaged[Stdcall]<int, int>) addr;
        // Initializing QueryStringNative
        addr = IntPtr.Zero;
        addr = getProcAddress("eglQueryString");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_QueryStringNative");
        _addr_QueryStringNative = (delegate* unmanaged[Stdcall]<nint, int, nint>) addr;
        // Initializing CreatePbufferFromClientBuffer
        addr = IntPtr.Zero;
        addr = getProcAddress("eglCreatePbufferFromClientBuffer");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_CreatePbufferFromClientBuffer");
        _addr_CreatePbufferFromClientBuffer = (delegate* unmanaged[Stdcall]<nint, int, nint, nint, int*, nint>) addr;
        // Initializing CreatePbufferFromClientBufferPtr
        addr = IntPtr.Zero;
        addr = getProcAddress("eglCreatePbufferFromClientBuffer");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_CreatePbufferFromClientBufferPtr");
        _addr_CreatePbufferFromClientBufferPtr = (delegate* unmanaged[Stdcall]<nint, int, nint, nint, int*, nint>) addr;
        // Initializing QueryDisplayAttribExt
        addr = IntPtr.Zero;
        addr = getProcAddress("eglQueryDisplayAttribEXT");
        _addr_QueryDisplayAttribExt = (delegate* unmanaged[Stdcall]<nint, int, nint*, int>) addr;
        // Initializing QueryDeviceAttribExt
        addr = IntPtr.Zero;
        addr = getProcAddress("eglQueryDeviceAttribEXT");
        _addr_QueryDeviceAttribExt = (delegate* unmanaged[Stdcall]<nint, int, nint*, int>) addr;
    }
}