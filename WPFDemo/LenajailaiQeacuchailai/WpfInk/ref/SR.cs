// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace WpfInk.@ref
{
    internal static partial class SR
    {
        public static string Get(string name)
        {
            return WpfCommon.src.System.SR.GetResourceString(name, null);
        }
        
        public static string Get(string name, params object[] args)
        {
            return WpfCommon.src.System.SR.Format(WpfCommon.src.System.SR.GetResourceString(name, null), args);
        }
    }
}
