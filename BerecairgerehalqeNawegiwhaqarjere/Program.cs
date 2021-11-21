using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    static class Debugger
    {
        static void Main(string[] args)
        {
            LaunchInternal();
        }

        [DllImport("QCall", CharSet = CharSet.Unicode)]
        private static extern bool LaunchInternal();
    }
}
