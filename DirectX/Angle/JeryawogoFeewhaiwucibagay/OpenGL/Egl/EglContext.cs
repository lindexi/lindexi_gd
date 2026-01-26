using System;

namespace JeryawogoFeewhaiwucibagay.OpenGL.Egl;

public record EglContext(EglDisplay EglDisplay, IntPtr Context)
{
    public EglInterface EglInterface => EglDisplay.EglInterface;

    //public GlInterface GlInterface { get; }

}