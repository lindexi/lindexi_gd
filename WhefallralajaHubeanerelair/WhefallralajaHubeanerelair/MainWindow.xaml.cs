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
        }

        private void AddRealTimeStylus()
        {
            var windowInteropHelper = new WindowInteropHelper(this);
            var handle = windowInteropHelper.Handle;

            Guid clsid = new Guid("{DECBDC16-E824-436e-872D-14E8C7BF7D8B}");
            Guid iid = new Guid("{C6C77F97-545E-4873-85F2-E0FEE550B2E9}");
            string licenseKey = "{CAAD7274-4004-44e0-8A17-D6F1919C443A}";
            _nativeIRealTimeStylus = (IRealTimeStylusNative)ComObjectCreator.CreateInstanceLicense(clsid, iid, licenseKey);

            _nativeIRealTimeStylus.SetHWND(handle);

            _nativeIRealTimeStylus.GetHWND(out var hWnd);

            var useMouseForInput = false;

            _nativeIRealTimeStylus.SetAllTabletsMode(useMouseForInput);

            _stylusSyncPluginNativeShim = new StylusSyncPluginNativeShim();
            IntPtr stylusSyncPluginNativeInterface =
                Marshal.GetComInterfaceForObject((object)_stylusSyncPluginNativeShim, typeof(StylusSyncPluginNative));

            var penImcRect = new PenImcRect()
            {
                Left = 0,
                Top = 0,
                Right = 0,
                Bottom = 0,
            };
            _nativeIRealTimeStylus.SetWindowInputRect(ref penImcRect);

            _nativeIRealTimeStylus.AddStylusSyncPlugin(0U, stylusSyncPluginNativeInterface);

            _nativeIRealTimeStylus.MultiTouchEnable(true);
            _nativeIRealTimeStylus.Enable(true);

            Marshal.Release(stylusSyncPluginNativeInterface);
        }

        private IRealTimeStylusNative? _nativeIRealTimeStylus;
        private StylusSyncPluginNativeShim? _stylusSyncPluginNativeShim;
    }
}
