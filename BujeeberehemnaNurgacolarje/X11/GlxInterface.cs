using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlankX11App.X11;
internal unsafe class GlxInterface
{
    private const string libGL = "libGL.so.1";

    public GlxInterface()
    {
        Initialize(SafeGetProcAddress);
    }

    private void Initialize(Func<string, nint> getProcAddress)
    {
        nint addr;
        // Initializing ChooseFBConfig
        addr = 0;
        addr = getProcAddress("glXChooseFBConfig");
        if (addr == default) throw new EntryPointNotFoundException("_addr_ChooseFBConfig");
        _addr_ChooseFBConfig = (delegate* unmanaged[Stdcall]<nint, int, int*, out int, nint*>)addr;
        // Initializing GetVisualFromFBConfig
        addr = 0;
        addr = getProcAddress("glXGetVisualFromFBConfig");
        if (addr == default) throw new EntryPointNotFoundException("_addr_GetVisualFromFBConfig");
        _addr_GetVisualFromFBConfig = (delegate* unmanaged[Stdcall]<nint, nint, XVisualInfo*>)addr;
    }

    delegate* unmanaged[Stdcall]<nint, int, int*, out int, nint*> _addr_ChooseFBConfig;
    public nint* ChooseFBConfig(nint @dpy, int @screen, int[] @attrib_list, out int @nelements)
    {
        fixed (int* @__p_attrib_list = attrib_list)
        {
            return _addr_ChooseFBConfig(@dpy, @screen, @__p_attrib_list, out @nelements);
        }
    }

    delegate* unmanaged[Stdcall]<nint, nint, XVisualInfo*> _addr_GetVisualFromFBConfig;
    public XVisualInfo* GetVisualFromFBConfig(nint @dpy, nint @config)
    {
        return _addr_GetVisualFromFBConfig(@dpy, @config);
    }


    // Ignores egl functions.
    // On some Linux systems, glXGetProcAddress will return valid pointers for even EGL functions.
    // This makes Skia try to load some data from EGL,
    // which can then cause segmentation faults because they return garbage.
    public static nint SafeGetProcAddress(string proc)
    {
        if (proc.StartsWith("egl", StringComparison.InvariantCulture))
        {
            return nint.Zero;
        }

        return GlxGetProcAddress(proc);
    }

    [DllImport(libGL, EntryPoint = "glXGetProcAddress")]
    public static extern nint GlxGetProcAddress(string buffer);
}
