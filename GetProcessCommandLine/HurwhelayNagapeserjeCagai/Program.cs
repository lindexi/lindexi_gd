using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace HurwhelayNagapeserjeCagai
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var process in Process.GetProcesses())
            {
                Console.WriteLine($"{process.ProcessName} {GetCommandLineOfProcess(process)}");
            }
        }

        public static string GetCommandLineOfProcess(Process process)
        {
            // max size of a command line is USHORT/sizeof(WCHAR), so we are going
            // just allocate max USHORT for sanity sake.
            var stringBuilder = new StringBuilder(0xFFFF);
            if (Environment.Is64BitProcess)
            {
                GetProcCmdLine64((uint) process.Id, stringBuilder, (uint) stringBuilder.Capacity);
            }
            else
            {
                GetProcCmdLine32((uint) process.Id, stringBuilder, (uint) stringBuilder.Capacity);
            }

            return stringBuilder.ToString();
        }

        [DllImport("ProcCmdLine32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetProcCmdLine")]
        private static extern bool GetProcCmdLine32(uint nProcId, StringBuilder stringBuilder, uint dwSizeBuf);

        [DllImport("ProcCmdLine64.dll", CharSet = CharSet.Unicode, EntryPoint = "GetProcCmdLine")]
        private static extern bool GetProcCmdLine64(uint nProcId, StringBuilder stringBuilder, uint dwSizeBuf);
    }
}