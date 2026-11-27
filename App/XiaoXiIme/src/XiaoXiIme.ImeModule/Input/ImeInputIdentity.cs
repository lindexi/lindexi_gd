using System.Runtime.InteropServices;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

internal static class ImeInputIdentity
{
    private static readonly string Owner = Guid.NewGuid().ToString("N");
    private static readonly string WindowProperty = "XiaoXiIme.Target." + Owner;
    private static long s_sequence;
    private static long s_windowIdentity;

    internal static IImmContextAccessor ContextAccessor { get; set; } = ImmContextAccessor.Instance;
    internal static bool UsesNativeContext => ReferenceEquals(ContextAccessor, ImmContextAccessor.Instance);
    internal static string OwnerId => Owner;
    internal static string WindowIdentityProperty => WindowProperty;
    internal static long NextSequence() => Interlocked.Increment(ref s_sequence);

    internal static unsafe ImeSessionId Resolve(HImc himc)
    {
        if (!OperatingSystem.IsWindows() || himc.Value == 0 || !UsesNativeContext)
        {
            return ImeSessionId.FromHimc(himc.Value);
        }

        var accessor = ContextAccessor;
        var pointer = accessor.LockInputContext(himc);
        if (pointer == 0)
        {
            throw new InvalidOperationException();
        }

        try
        {
            var context = (InputContext*)pointer;
            var handle = accessor.ResizePrivateData(context->HPrivate, (uint)sizeof(ImePrivateData));
            if (handle == 0)
            {
                throw new InvalidOperationException();
            }

            context->HPrivate = handle;
            var data = (ImePrivateData*)accessor.LockPrivateData(handle);
            if (data == null)
            {
                throw new InvalidOperationException();
            }

            Guid identity;
            try
            {
                if (data->ContextIdentity == Guid.Empty)
                {
                    data->ContextIdentity = Guid.NewGuid();
                }

                identity = data->ContextIdentity;
            }
            finally
            {
                accessor.UnlockPrivateData(handle);
            }

            // Window properties belong to the window object and disappear on real destruction.
            var window = context->Hwnd.Value;
            var windowIdentity = window == 0 ? 0 : GetProp(window, WindowProperty);
            if (window != 0 && windowIdentity == 0)
            {
                windowIdentity = (nint)Interlocked.Increment(ref s_windowIdentity);
                if (!SetProp(window, WindowProperty, windowIdentity))
                {
                    throw new InvalidOperationException();
                }
            }

            return new($"imm:{Owner}:{identity:N}");
        }
        finally
        {
            accessor.UnlockInputContext(himc);
        }
    }

    [DllImport("user32.dll", EntryPoint = "GetPropW", CharSet = CharSet.Unicode)]
    private static extern nint GetProp(nint window, string name);

    [DllImport("user32.dll", EntryPoint = "SetPropW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProp(nint window, string name, nint value);
}