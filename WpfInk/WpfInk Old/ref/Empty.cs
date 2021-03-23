using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using MS.Internal.PresentationCore;

namespace WpfInk
{

}

namespace MS.Utility
{

}

namespace MS.Internal.IO.Packaging
{

}

namespace MS.Internal.Ink.InkSerializedFormat
{
    public static class StylusPointCollectionExtension
    {
        public static void ToISFReadyArrays(this StylusPointCollection stroke, out int[][] output, out bool shouldPersistPressure)
        {
            output = new int[10][];
            shouldPersistPressure = false;
        }

        public static int[] GetAdditionalData(this StylusPoint stroke)
        {
            return new int[0];
        }
    }

    public static class StylusPointDescriptionExtension
    {
        public static Guid[] GetStylusPointPropertyIds(this StylusPointDescription s)
        {
            return new Guid[0];
        }
    }
}

namespace MS.Internal.WindowsBase
{
    internal static partial class SR
    {
        public static string Get(string name)
        {
            return GetResourceString(name, null);
        }

        public static string Get(string name, params object[] args)
        {
            return Format(GetResourceString(name, null), args);
        }
    }

    internal partial class SR
    {
        private static ResourceManager ResourceManager => SRID.ResourceManager;

        // This method is used to decide if we need to append the exception message parameters to the message when calling SR.Format.
        // by default it returns false.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool UsingResourceKeys()
        {
            return false;
        }

        internal static string GetResourceString(string resourceKey, string defaultString)
        {
            string resourceString = null;
            try { resourceString = ResourceManager.GetString(resourceKey); }
            catch (MissingManifestResourceException) { }

            if (defaultString != null && resourceKey.Equals(resourceString, StringComparison.Ordinal))
            {
                return defaultString;
            }

            return resourceString;
        }

        internal static string Format(string resourceFormat, params object[] args)
        {
            if (args != null)
            {
                if (UsingResourceKeys())
                {
                    return resourceFormat + string.Join(", ", args);
                }

                return string.Format(resourceFormat, args);
            }

            return resourceFormat;
        }

        internal static string Format(string resourceFormat, object p1)
        {
            if (UsingResourceKeys())
            {
                return string.Join(", ", resourceFormat, p1);
            }

            return string.Format(resourceFormat, p1);
        }

        internal static string Format(string resourceFormat, object p1, object p2)
        {
            if (UsingResourceKeys())
            {
                return string.Join(", ", resourceFormat, p1, p2);
            }

            return string.Format(resourceFormat, p1, p2);
        }

        internal static string Format(string resourceFormat, object p1, object p2, object p3)
        {
            if (UsingResourceKeys())
            {
                return string.Join(", ", resourceFormat, p1, p2, p3);
            }

            return string.Format(resourceFormat, p1, p2, p3);
        }
    }
}

namespace System.Windows.Media.Composition
{
    
}