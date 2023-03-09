using System;
using System.Runtime.InteropServices;

namespace LightTextEditorPlus.Utils
{
    internal class Win32
    {
        public class User32
        {
            public const string LibraryName = "user32";

            /// <summary>
            /// 获取当前系统中被激活的窗口
            /// </summary>
            /// <returns></returns>
            [DllImport(LibraryName, ExactSpelling = true)]
            public static extern IntPtr GetForegroundWindow();
        }
    }
}
