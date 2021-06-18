using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using PInvoke;

namespace LalljemjehawjogarHecheneelere
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Signature.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;

            Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// 设置点击穿透到后面透明的窗口
        /// </summary>
        public void SetTransparentHitThrough()
        {
            var windowInteropHelper = new WindowInteropHelper(this);
            int GWL_EXSTYLE = -20;
            int WS_EX_TRANSPARENT = 32;

            User32.SetWindowLongPtr(windowInteropHelper.Handle, User32.WindowLongIndexFlags.GWL_EXSTYLE,
                (IntPtr) (int) ((long) User32.GetWindowLong(windowInteropHelper.Handle,
                    User32.WindowLongIndexFlags.GWL_EXSTYLE) | (long) WS_EX_TRANSPARENT));
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetTransparentHitThrough();

            ((HwndSource)PresentationSource.FromVisual(this)).AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                //想要让窗口透明穿透鼠标和触摸等，需要同时设置 WS_EX_LAYERED 和 WS_EX_TRANSPARENT 样式，
                //确保窗口始终有 WS_EX_LAYERED 这个样式，并在开启穿透时设置 WS_EX_TRANSPARENT 样式
                //但是WPF窗口在未设置 AllowsTransparency = true 时，会自动去掉 WS_EX_LAYERED 样式（在 HwndTarget 类中)，
                //如果设置了 AllowsTransparency = true 将使用WPF内置的低性能的透明实现，
                //所以这里通过 Hook 的方式，在不使用WPF内置的透明实现的情况下，强行保证这个样式存在。
                if (msg == (int)User32.WindowMessage.WM_STYLECHANGED && (long)wParam == (long)-20)
                {
                    var styleStruct = (STYLESTRUCT)Marshal.PtrToStructure(lParam, typeof(STYLESTRUCT));
                    int WS_EX_LAYERED = 524288;
                    styleStruct.styleNew |= WS_EX_LAYERED;
                    Marshal.StructureToPtr(styleStruct, lParam, false);
                    handled = true;
                }
                return IntPtr.Zero;
            });
        }

        private struct STYLESTRUCT
        {
            public int styleOld;
            public int styleNew;
        }
    }
}
