namespace AngleOpenGLDemo.OpenGL.Egl;

public record EglContext(EglDisplay EglDisplay, IntPtr Context, GlVersion Version)
{
    public EglInterface EglInterface => EglDisplay.EglInterface;

    public GlInterface GlInterface
    {
        get
        {
            if (_glInterface is null)
            {
                _glInterface = GlInterface.FromNativeUtf8GetProcAddress(Version, EglInterface.GetProcAddress);
            }

            return _glInterface;
        }
    }

    private GlInterface? _glInterface;

    public IDisposable MakeCurrent() => MakeCurrent(OffscreenSurface);

    public IDisposable MakeCurrent(EglSurface? surface)
    {
        var locker = new object();
        Monitor.Enter(locker);
        var old = new RestoreContext(EglInterface, EglDisplay.Handle, locker);

        EglInterface.MakeCurrent(EglDisplay.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        var success = EglInterface.MakeCurrent(EglDisplay.Handle, surface?.DangerousGetHandle() ?? IntPtr.Zero, surface?.DangerousGetHandle() ?? IntPtr.Zero, Context);

        if (!success)
        {
            var error = EglInterface.GetError();

            throw OpenGlException.GetFormattedEglException("eglMakeCurrent", error);
        }

        return old;
    }

    public EglSurface? OffscreenSurface { get; } = null;

    private class RestoreContext : IDisposable
    {
        private readonly EglInterface _egl;
        private readonly object _l;
        private readonly IntPtr _display;
        private readonly IntPtr _context, _read, _draw;

        public RestoreContext(EglInterface egl, IntPtr defDisplay, object l)
        {
            _egl = egl;
            _l = l;
            _display = _egl.GetCurrentDisplay();
            if (_display == IntPtr.Zero)
                _display = defDisplay;
            _context = _egl.GetCurrentContext();
            _read = _egl.GetCurrentSurface(EglConsts.EGL_READ);
            _draw = _egl.GetCurrentSurface(EglConsts.EGL_DRAW);
        }

        public void Dispose()
        {
            _egl.MakeCurrent(_display, _draw, _read, _context);
            Monitor.Exit(_l);
        }

    }
}