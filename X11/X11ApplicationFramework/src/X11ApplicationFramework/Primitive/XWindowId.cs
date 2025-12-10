using System;

namespace X11ApplicationFramework.Primitive;

public readonly record struct XWindowId(IntPtr Handle)
{
    public static implicit operator IntPtr(XWindowId window)
    {
        return window.Handle;
    }
}