using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace WhefallralajaHubeanerelair
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            TextBlock = new TextBlock()
            {
                Margin = new Thickness(10, 10, 10, 10),
                VerticalAlignment = VerticalAlignment.Bottom,
                TextWrapping = TextWrapping.Wrap
            };

            ((Grid) Content).Children.Add(TextBlock);

            //Activated += MainWindow_Activated;

            //TouchMove += MainWindow_TouchMove;
        }

        //private void MainWindow_TouchMove(object? sender, System.Windows.Input.TouchEventArgs e)
        //{
        //    TextBlock.Text += $"MainWindow_TouchMove\r\n";
        //}

        //private void MainWindow_Activated(object? sender, EventArgs e)
        //{
        //    //var windowInteropHelper = new WindowInteropHelper(this);
        //    //var handle = windowInteropHelper.Handle;

        //    //_nativeIRealTimeStylus!.Enable(false);
        //    //_nativeIRealTimeStylus.SetHWND(handle);
        //    //_nativeIRealTimeStylus.Enable(true);

        //    _nativeIRealTimeStylus.IsEnabled(out var value);
        //    TextBlock.Text += $"Enable = {value}\r\n";

        //    _nativeIRealTimeStylus.GetWindowInputRect(out var rect);
        //    TextBlock.Text += $"Enable = {rect.Left} {rect.Top} {rect.Right} {rect.Bottom}\r\n";

        //    //AddRealTimeStylus();
        //}

        public static TextBlock TextBlock { private set; get; } = null!;

        protected override void OnSourceInitialized(EventArgs e)
        {
            AddRealTimeStylus();

            // 测试接收的消息
            var windowInteropHelper = new WindowInteropHelper(this);
            var handle = windowInteropHelper.Handle;
            HwndSource source = HwndSource.FromHwnd(handle)!;
            source.AddHook(Hook);
        }

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterTouchWindow(System.IntPtr hWnd, uint ulFlags);

        private void AddRealTimeStylus()
        {
            var windowInteropHelper = new WindowInteropHelper(this);
            var handle = windowInteropHelper.Handle;

            // 这个注册是没有用的，注册也不能收到消息
            //RegisterTouchWindow(handle, 0);

            Guid clsid = new Guid("{DECBDC16-E824-436e-872D-14E8C7BF7D8B}");
            Guid iid = new Guid("{C6C77F97-545E-4873-85F2-E0FEE550B2E9}");
            string licenseKey = "{CAAD7274-4004-44e0-8A17-D6F1919C443A}";
            _nativeIRealTimeStylus = (IRealTimeStylus)ComObjectCreator.CreateInstanceLicense(clsid, iid, licenseKey);

            _nativeIRealTimeStylus.SetHWND(handle);

            _nativeIRealTimeStylus.GetHWND(out var hWnd);

            var useMouseForInput = true;

            _nativeIRealTimeStylus.SetAllTabletsMode(useMouseForInput);

            _stylusSyncPluginNativeShim = new StylusSyncPluginNativeShim();
            IntPtr stylusSyncPluginNativeInterface =
                Marshal.GetComInterfaceForObject((object)_stylusSyncPluginNativeShim, typeof(IStylusSyncPluginNative2));

            var penImcRect = new PenImcRect()
            {
                Left = 0,
                Top = 0,
                Right = 0,
                Bottom = 0,
            };
            _nativeIRealTimeStylus.SetWindowInputRect(ref penImcRect);

            _nativeIRealTimeStylus.AddStylusSyncPlugin(0U, stylusSyncPluginNativeInterface);

            _nativeIRealTimeStylus.AllTouchEnable(true);
            _nativeIRealTimeStylus.MultiTouchEnable(false);
            _nativeIRealTimeStylus.Enable(true);

            // 别忘了减少引用计数。这里的 Release 其实不是释放的意思，仅仅只是减少引用计数
            Marshal.Release(stylusSyncPluginNativeInterface);
        }

        private IRealTimeStylus? _nativeIRealTimeStylus;
        private StylusSyncPluginNativeShim? _stylusSyncPluginNativeShim;

        private const int WM_POINTERDOWN = 0x0246;
        private const int WM_TOUCH = 0x0240;
        private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == WM_TOUCH)
            {
                TextBlock.Text += "WM_TOUCH \r\n";
            }
            else if (msg == WM_POINTERDOWN)
            {
                TextBlock.Text += "WM_Pointer \r\n";
            }

            return IntPtr.Zero;
        }
    }
}
