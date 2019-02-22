using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace HurwhelayNagapeserjeCagai
{
    internal class Program
    {
        public static string GetCommandLineOfProcess(int processId)
        {
            var pid = processId;

            var pbi = new NativeMethods.PROCESS_BASIC_INFORMATION();

            IntPtr proc = NativeMethods.OpenProcess
            (
                PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid
            );

            if (proc == IntPtr.Zero)
            {
                return "";
            }

            if (NativeMethods.NtQueryInformationProcess(proc, 0, ref pbi, pbi.Size, IntPtr.Zero) == 0)
            {
                var buff = new byte[IntPtr.Size];
                if (NativeMethods.ReadProcessMemory
                (
                    proc,
                    (IntPtr) (pbi.PebBaseAddress.ToInt32() + 0x10),
                    buff,
                    IntPtr.Size, out _
                ))
                {
                    var buffPtr = BitConverter.ToInt32(buff, 0);
                    var commandLine = new byte[Marshal.SizeOf(typeof(NativeMethods.UNICODE_STRING))];

                    if
                    (
                        NativeMethods.ReadProcessMemory
                        (
                            proc, (IntPtr) (buffPtr + 0x40),
                            commandLine,
                            Marshal.SizeOf(typeof(NativeMethods.UNICODE_STRING)), out _
                        )
                    )
                    {
                        var ucsData = ByteArrayToStructure<NativeMethods.UNICODE_STRING>(commandLine);
                        var parms = new byte[ucsData.Length];
                        if
                        (
                            NativeMethods.ReadProcessMemory
                            (
                                proc, ucsData.buffer, parms,
                                ucsData.Length, out _
                            )
                        )
                        {
                            return Encoding.Unicode.GetString(parms);
                        }
                    }
                }
            }

            NativeMethods.CloseHandle(proc);

            return "";
        }

        [STAThread]
        private static void Main(string[] args)
        {
            if (Environment.Is64BitProcess)
            {
                throw new InvalidOperationException("暂时只支持x86程序");
            }

            foreach (var process in Process.GetProcesses())
            {
                Console.WriteLine($"{process.ProcessName} {GetCommandLineOfProcess(process.Id)}");
            }
        }


        private const uint PROCESS_QUERY_INFORMATION = 0x400;
        private const uint PROCESS_VM_READ = 0x010;

        private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var stuff = (T) Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return stuff;
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern IntPtr OpenProcess
            (
                uint dwDesiredAccess,
                [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
                int dwProcessId
            );


            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool ReadProcessMemory
            (
                IntPtr hProcess,
                IntPtr lpBaseAddress,
                byte[] lpBuffer,
                int nSize,
                out IntPtr lpNumberOfBytesRead
            );

            [DllImport("ntdll.dll")]
            internal static extern int NtQueryInformationProcess
            (
                IntPtr ProcessHandle,
                uint ProcessInformationClass,
                ref PROCESS_BASIC_INFORMATION ProcessInformation,
                uint ProcessInformationLength,
                IntPtr ReturnLength
            );

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            internal struct PROCESS_BASIC_INFORMATION
            {
                internal int ExitProcess;
                internal IntPtr PebBaseAddress;
                internal IntPtr AffinityMask;
                internal int BasePriority;
                internal IntPtr UniqueProcessId;
                internal IntPtr InheritedFromUniqueProcessId;

                internal uint Size => (uint) Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION));
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            internal struct UNICODE_STRING
            {
                internal ushort Length;
                internal ushort MaximumLength;
                internal IntPtr buffer;
            }
        }
    }
}