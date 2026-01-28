using System;

namespace KurbawjeleJarlayenel.OpenGL;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal class GlExtensionEntryPoint : Attribute
{
    public GlExtensionEntryPoint(string entry, string extension)
    {
    }

    public GlExtensionEntryPoint(string entry, string extension, GlProfileType profile)
    {
    }

    public static IntPtr GetProcAddress(Func<string, IntPtr> getProcAddress, GlInterface.GlContextInfo context,
        string entry, string extension, GlProfileType? profile = null)
    {
        // Ignore different profile type
        if (profile.HasValue && profile != context.Version.Type)
            return IntPtr.Zero;

        // Check if extension is supported by the current context
        if (!context.Extensions.Contains(extension))
            return IntPtr.Zero;

        return getProcAddress(entry);
    }
}