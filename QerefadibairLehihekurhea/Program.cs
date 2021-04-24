using System;
using Microsoft.Win32;

namespace QerefadibairLehihekurhea
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"SystemManufacturer={Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS").GetValue("SystemManufacturer")}");
            Console.WriteLine($"SystemProductName={Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS").GetValue("SystemProductName")}");
            Console.Read();
        }
    }
}