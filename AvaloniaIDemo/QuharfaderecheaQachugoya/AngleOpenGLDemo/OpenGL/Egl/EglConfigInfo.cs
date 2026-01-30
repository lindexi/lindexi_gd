using System;

namespace JeryawogoFeewhaiwucibagay.OpenGL.Egl;

internal class EglConfigInfo
{
    public IntPtr Config { get; }
    public GlVersion Version { get; }
    public int SurfaceType { get; }
    public int[] Attributes { get; }
    public int SampleCount { get; }
    public int StencilSize { get; }

    public EglConfigInfo(IntPtr config, GlVersion version, int surfaceType, int[] attributes, int sampleCount,
        int stencilSize)
    {
        Config = config;
        Version = version;
        SurfaceType = surfaceType;
        Attributes = attributes;
        SampleCount = sampleCount;
        StencilSize = stencilSize;
    }
}