using System;
using System.Linq;
using System.Threading;

using static JeryawogoFeewhaiwucibagay.OpenGL.Egl.EglConsts;

namespace JeryawogoFeewhaiwucibagay.OpenGL.Egl;

public class EglDisplay : IDisposable
{
    public EglDisplay(IntPtr display, EglInterface eglInterface)
    {
        EglInterface = eglInterface;
        _display = display;

        _config = InitializeAndGetConfig(display, eglInterface);
    }

    public IntPtr Config => _config.Config;

    private readonly EglConfigInfo _config;

    public int StencilSize => _config.StencilSize;

    private static EglConfigInfo InitializeAndGetConfig(IntPtr display, EglInterface eglInterface)
    {
        if (!eglInterface.Initialize(display, out _, out _))
        {
            throw OpenGlException.GetFormattedException("eglInitialize", eglInterface);
        }

        GlVersion[] versions = new[]
        {
            new GlVersion(GlProfileType.OpenGLES, 3, 0),
            new GlVersion(GlProfileType.OpenGLES, 2, 0)
        };

        var cfgs = versions
            .Where(x => x.Type == GlProfileType.OpenGLES)
            .Select(x =>
            {
                var typeBit = EGL_OPENGL_ES3_BIT;

                switch (x.Major)
                {
                    case 2:
                        typeBit = EGL_OPENGL_ES2_BIT;
                        break;

                    case 1:
                        typeBit = EGL_OPENGL_ES_BIT;
                        break;
                }

                return new
                {
                    Attributes = new[]
                    {
                        EGL_CONTEXT_MAJOR_VERSION, x.Major,
                        EGL_CONTEXT_MINOR_VERSION, x.Minor,
                        EGL_NONE
                    },
                    Api = EGL_OPENGL_ES_API,
                    RenderableTypeBit = typeBit,
                    Version = x
                };
            });

        foreach (var cfg in cfgs)
        {
            if (!eglInterface.BindApi(cfg.Api))
                continue;
            foreach (var surfaceType in new[] { EGL_PBUFFER_BIT | EGL_WINDOW_BIT, EGL_WINDOW_BIT })
            foreach (var stencilSize in new[] { 8, 1, 0 })
            foreach (var depthSize in new[] { 8, 1, 0 })
            {
                var attribs = new[]
                {
                    EGL_SURFACE_TYPE, surfaceType,
                    EGL_RENDERABLE_TYPE, cfg.RenderableTypeBit,
                    EGL_RED_SIZE, 8,
                    EGL_GREEN_SIZE, 8,
                    EGL_BLUE_SIZE, 8,
                    EGL_ALPHA_SIZE, 8,
                    EGL_STENCIL_SIZE, stencilSize,
                    EGL_DEPTH_SIZE, depthSize,
                    EGL_NONE
                };
                if (!eglInterface.ChooseConfig(display, attribs, out var config, 1, out int numConfigs))
                    continue;
                if (numConfigs == 0)
                    continue;


                eglInterface.GetConfigAttrib(display, config, EGL_SAMPLES, out var sampleCount);
                eglInterface.GetConfigAttrib(display, config, EGL_STENCIL_SIZE, out var returnedStencilSize);
                return new EglConfigInfo(config, cfg.Version, surfaceType, cfg.Attributes, sampleCount,
                    returnedStencilSize);
            }
        }

        throw new OpenGlException("No suitable EGL config was found");
    }

    public EglInterface EglInterface { get; }

    public IntPtr Handle => _display;

    private IntPtr _display;

    public Lock.Scope Lock() => _lock.EnterScope();

    private readonly Lock _lock = new();

    public unsafe EglSurface CreatePBufferFromClientBuffer(int bufferType, IntPtr handle, int* attribs)
    {
        using (Lock())
        {
            var s = EglInterface.CreatePbufferFromClientBufferPtr(Handle, bufferType, handle,
                Config, attribs);

            if (s == IntPtr.Zero)
                throw OpenGlException.GetFormattedException("eglCreatePbufferFromClientBuffer", EglInterface);
            return new EglSurface(this, s);
        }
    }

    public EglContext CreateContext()
    {
        lock (_lock)
        {
            var context = EglInterface.CreateContext(_display, Config,  IntPtr.Zero, _config.Attributes);

            if (context == IntPtr.Zero)
            {
                throw OpenGlException.GetFormattedException("eglCreateContext", EglInterface);
            }

            return new EglContext(this, context, _config.Version);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_display != IntPtr.Zero)
            {
                EglInterface.Terminate(_display);
            }

            _display = IntPtr.Zero;
        }
    }
}