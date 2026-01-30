using AngleOpenGLDemo.OpenGL.Egl;
using static AngleOpenGLDemo.OpenGL.Egl.EglConsts;

namespace AngleOpenGLDemo.OpenGL.Angle;

class AngleWin32EglDisplay : EglDisplay
{
    public AngleWin32EglDisplay(IntPtr angleDisplay, Win32AngleEglInterface egl) : base(angleDisplay, egl)
    {
        _angleDisplay = angleDisplay;
        _egl = egl;

        var extensions = egl.QueryString(angleDisplay, EGL_EXTENSIONS);
        _flexibleSurfaceSupported = extensions?.Contains("EGL_ANGLE_flexible_surface_compatibility") ?? false;
    }

    private readonly IntPtr _angleDisplay;

    private readonly Win32AngleEglInterface _egl;
    private readonly bool _flexibleSurfaceSupported;

    public unsafe EglSurface WrapDirect3D11Texture(IntPtr handle, int offsetX, int offsetY, int width, int height)
    {
        var attrs = stackalloc[]
        {
            EGL_WIDTH, width, EGL_HEIGHT, height, EGL_TEXTURE_OFFSET_X_ANGLE, offsetX,
            EGL_TEXTURE_OFFSET_Y_ANGLE, offsetY,
            _flexibleSurfaceSupported ? EGL_FLEXIBLE_SURFACE_COMPATIBILITY_SUPPORTED_ANGLE : EGL_NONE, EGL_TRUE,
            EGL_NONE
        };

        return CreatePBufferFromClientBuffer(EGL_D3D_TEXTURE_ANGLE, handle, attrs);
    }
}