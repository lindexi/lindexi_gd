using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace GetProcCmdLine.Example
{
    public static class ProcCmdLine
    {
        private static class Native
        {
            [DllImport("shell32.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr CommandLineToArgvW(string lpCmdLine, out int pNumArgs);

            [DllImport("ProcCmdLine32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetProcCmdLine")]
            public extern static bool GetProcCmdLine32(uint nProcId, StringBuilder sb, uint dwSizeBuf);

            [DllImport("ProcCmdLine64.dll", CharSet = CharSet.Unicode, EntryPoint = "GetProcCmdLine")]
            public extern static bool GetProcCmdLine64(uint nProcId, StringBuilder sb, uint dwSizeBuf);
        }

        private static string[] CommandLineToArgs(string commandLine)
        {
            int argc;
            var argv = Native.CommandLineToArgvW(commandLine, out argc);
            if (argv == IntPtr.Zero) { throw new System.ComponentModel.Win32Exception(); }
            try
            {
                var args = new string[argc];
                for (var i = 0; i < args.Length; ++i)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }
                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }

        public static string GetCommandLineOfProcess(Process proc)
        {
            // max size of a command line is USHORT/sizeof(WCHAR), so we are going
            // just max USHORT for sanity's sake.
            var sb = new StringBuilder(0xFFFF);
            switch (IntPtr.Size)
            {
                case 4: Native.GetProcCmdLine32((uint)proc.Id, sb, (uint)sb.Capacity); break;
                case 8: Native.GetProcCmdLine64((uint)proc.Id, sb, (uint)sb.Capacity); break;
            }
            return sb.ToString();
        }

        public static string GetCommandLineOfProcessIgnoreFirst(Process proc)
        {
            var t = GetCommandLineArrayOfProcess(proc).ToList();
            t.RemoveAt(0);
            return RebuildArgumentsFromArray(t.ToArray());
        }

        public static string[] GetCommandLineArrayOfProcess(Process proc)
        {
            return CommandLineToArgs(GetCommandLineOfProcess(proc));
        }

        private static string RebuildArgumentsFromArray(string[] arrArgs)
        {
            Func<string, string> encode = (s) =>
            {
                if (string.IsNullOrEmpty(s)) { return "\"\""; }
                return Regex.Replace(Regex.Replace(s, @"(\\*)" + "\"", @"$1\$0"), @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"");
            };

            if ((arrArgs != null) && (arrArgs.Length > 0))
            {
                return string.Join(" ", Array.ConvertAll(arrArgs, (el) => encode(el)));
            }
            return string.Empty;
        }
    }
}
