using System;
using System.Collections.Generic;
using System.Text;

namespace KurbawjeleJarlayenel.OpenGL;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal class GlMinVersionEntryPoint : Attribute
{
    public GlMinVersionEntryPoint(string entry, int minVersionMajor, int minVersionMinor)
    {
    }

    public GlMinVersionEntryPoint(string entry, int minVersionMajor, int minVersionMinor, GlProfileType profile)
    {
    }

    public static IntPtr GetProcAddress(Func<string, IntPtr> getProcAddress, GlInterface.GlContextInfo context,
        string entry, int minVersionMajor, int minVersionMinor, GlProfileType? profile = null)
    {
        if (profile.HasValue && context.Version.Type != profile)
            return IntPtr.Zero;
        if (context.Version.Major < minVersionMajor)
            return IntPtr.Zero;
        if (context.Version.Major == minVersionMajor && context.Version.Minor < minVersionMinor)
            return IntPtr.Zero;
        return getProcAddress(entry);
    }
}