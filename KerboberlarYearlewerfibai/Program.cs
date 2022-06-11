using System;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Vanara.PInvoke;
using Microsoft.Win32;

namespace KerboberlarYearlewerfibai
{
    public class Program
    {
        static void Main(string[] args)
        {
            var program = new Program();
            program.GetSystemFirmwareTable();
            Console.WriteLine(Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS")?.GetValue("SystemProductName")??"null");
        }

        [Benchmark]
        public void GetSystemFirmwareTable()
        {
            var systemFirmwareTableSize = Kernel32.GetSystemFirmwareTable(Kernel32.FirmwareTableProviderId.RSMB, 0, IntPtr.Zero, 0);

            var buffer = new byte[systemFirmwareTableSize];

            unsafe
            {
                fixed (byte* p = buffer)
                {
                    IntPtr ptr = new IntPtr(p);
                    Kernel32.GetSystemFirmwareTable(Kernel32.FirmwareTableProviderId.RSMB, 0, ptr,
                        systemFirmwareTableSize);

                    var x = *(RawSMBIOSData*)p;

                    var smBIOSTableDataLength = x.Length;
                    var pSMBIOSTableData = p + 8;
                    var str = Encoding.ASCII.GetString(pSMBIOSTableData, (int)smBIOSTableDataLength);
                    Console.WriteLine(str);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RawSMBIOSData
    {
        public byte Used20CallingMethod;
        public byte SMBIOSMajorVersion;
        public byte SMBIOSMinorVersion;
        public byte DmiRevision;
        public UInt32 Length;
        //public byte[] SMBIOSTableData;
    }
}
