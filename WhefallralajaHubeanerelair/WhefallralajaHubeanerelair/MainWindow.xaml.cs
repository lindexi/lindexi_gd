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
        }

        public static TextBlock TextBlock { private set; get; } = null!;

        protected override void OnSourceInitialized(EventArgs e)
        {
            var windowInteropHelper = new WindowInteropHelper(this);
            var handle = windowInteropHelper.Handle;

            Guid clsid = new Guid("{DECBDC16-E824-436e-872D-14E8C7BF7D8B}");
            Guid iid = new Guid("{C6C77F97-545E-4873-85F2-E0FEE550B2E9}");
            string licenseKey = "{CAAD7274-4004-44e0-8A17-D6F1919C443A}";
            _nativeIRealTimeStylus = (IRealTimeStylusNative) ComObjectCreator.CreateInstanceLicense(clsid, iid, licenseKey);

            _nativeIRealTimeStylus.SetHWND(handle);

            _nativeIRealTimeStylus.GetHWND(out var hWnd);

            var useMouseForInput = true;

            _nativeIRealTimeStylus.SetAllTabletsMode(useMouseForInput);

            _stylusSyncPluginNativeShim = new StylusSyncPluginNativeShim();
            IntPtr interfaceForObject1 = Marshal.GetComInterfaceForObject((object) _stylusSyncPluginNativeShim, typeof(StylusSyncPluginNative));

            _nativeIRealTimeStylus.AddStylusSyncPlugin(0U, interfaceForObject1);

            _nativeIRealTimeStylus.MultiTouchEnable(true);
            _nativeIRealTimeStylus.Enable(true);
        }

        private IRealTimeStylusNative? _nativeIRealTimeStylus;
        private StylusSyncPluginNativeShim? _stylusSyncPluginNativeShim;
    }
}
