using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Win32.SafeHandles;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace KeljawbofurhurQaibuwhanemlemba
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            const uint readAndWriteAccess = unchecked((uint) (1 << 30 | 1 << 31));
            var handle = CreateFileW(@"\\.\pipe\FooPipe", readAndWriteAccess, 0/*FILE_SHARE_NONE*/, IntPtr.Zero, 4/*OPEN_ALWAYS*/, 0/*SECURITY_ANONYMOUS*/, IntPtr.Zero);
            using (SafeFileHandle safeHandle = new SafeFileHandle(handle, true))
            {
                using (var fileStream = new FileStream(safeHandle, FileAccess.ReadWrite))
                {
                    using (var streamWriter = new StreamWriter(fileStream, Encoding.UTF8))
                    {
                        var streamReader = new StreamReader(fileStream, Encoding.UTF8);

                        while (true)
                        {
                            streamWriter.WriteLine("测试发送信息");
                            streamWriter.Flush();

                            var message = streamReader.ReadLine();
                            Debug.WriteLine(message);
                        }
                    }
                }
            }
        }

        [DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern IntPtr CreateFileW(
           [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            [Optional] IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);
    }
}
