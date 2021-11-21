using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GabagearledeHembijenear
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            SingleInstanceApp.EnsureSingleInstance("123123123", e.Args);
        }
    }

    public static class SingleInstanceApp
    {
        public static unsafe void EnsureSingleInstance(string applicationName, string[] args)
        {
            //MemoryMappedFile
            _mutex = new Mutex(true, "SINGLEINSTANCE_APP_Test", out var createdNew);

            PostMessage((IntPtr)HNDL_BROADCAST, SINGLEINSTANCE_APP, (IntPtr)1,
                new My_lParam()
                {
                    Text =  "sdfasdfawsdfasdf"
                });
        }


        [DllImport("user32", CharSet = CharSet.Auto)]
        public static extern int RegisterWindowMessage(string msg);

        public static readonly int SINGLEINSTANCE_APP =
            RegisterWindowMessage("SINGLEINSTANCE_APP_Test");

        static Mutex _mutex;
        public const int HNDL_BROADCAST = 0xffff;

        [DllImport("user32")]
        public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, My_lParam lparam);

       
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct My_lParam
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string Text;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct My_lParam2
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public char Text;
    }
}
