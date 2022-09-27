using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LightTextEditorPlus.TextEditorPlus.Utils
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
