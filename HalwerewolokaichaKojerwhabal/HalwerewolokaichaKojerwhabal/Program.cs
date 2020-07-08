using System;
using System.Diagnostics;
using PInvoke;

namespace HalwerewolokaichaKojerwhabal
{
    class Program
    {
        static void Main(string[] args)
        {
            var startupInfo = new Kernel32.STARTUPINFO()
            {
                dwX = 300, // X
                dwY = 300, // Y
                dwXSize = 1000, // 宽度
                dwYSize = 300,  // 高度
                dwFlags = Kernel32.StartupInfoFlags.STARTF_USESHOWWINDOW,
            };
             
            Kernel32.PROCESS_INFORMATION processInformation;
            var creationFlag = Kernel32.CreateProcessFlags.NORMAL_PRIORITY_CLASS | Kernel32.CreateProcessFlags.CREATE_UNICODE_ENVIRONMENT;

            var processSecurityAttribute = Kernel32.SECURITY_ATTRIBUTES.Create();
            var threadAttribute = Kernel32.SECURITY_ATTRIBUTES.Create();

            Kernel32.CreateProcess
            (
                lpApplicationName: @"C:\windows\notepad.exe",
                lpCommandLine: " ",
                lpProcessAttributes: processSecurityAttribute,
                lpThreadAttributes: threadAttribute, 
                bInheritHandles: false,
                dwCreationFlags: creationFlag,
                lpEnvironment: IntPtr.Zero,
                lpCurrentDirectory: null,
                lpStartupInfo: ref startupInfo,
                lpProcessInformation: out processInformation
            );

            Console.WriteLine(Kernel32.GetLastError());

            var process = Process.GetProcessById(processInformation.dwProcessId);
            Console.WriteLine(process.Id);
        }
    }
}
