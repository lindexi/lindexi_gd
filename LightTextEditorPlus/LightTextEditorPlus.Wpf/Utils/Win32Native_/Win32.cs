using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace LightTextEditorPlus.Utils
{
    internal class Win32
    {
        public class User32
        {
            public const string LibraryName = "user32";

            /// <summary>
            /// 获取系统度量值
            /// </summary>
            /// <param name="smIndex"></param>
            /// <returns></returns>
            [DllImport(LibraryName)]
            public static extern int GetSystemMetrics(SystemMetric smIndex);

            internal enum SystemMetric
            {
                SM_CXDOUBLECLK = 36, // 0x24
                SM_CYDOUBLECLK = 37, // 0x25
            }

            /// <summary>
            /// 获取鼠标双击事件两次点击的时间间隔
            /// </summary>
            /// <returns></returns>
            [DllImport(LibraryName)]
            public static extern uint GetDoubleClickTime();

            /// <summary>
            /// 获取当前系统中被激活的窗口
            /// </summary>
            /// <returns></returns>
            [DllImport(LibraryName, ExactSpelling = true)]
            public static extern IntPtr GetForegroundWindow();

            /// <summary>
            /// 获取光标闪烁时间
            /// </summary>
            /// <returns>毫秒</returns>
            [DllImport(LibraryName)]
            public static extern int GetCaretBlinkTime();
        }
    }
}
