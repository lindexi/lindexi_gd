using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Linearstar.Windows.RawInput;

namespace RaicheadoherneanuNalokearwherno;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        SourceInitialized += MainWindow_SourceInitialized;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        var windowInteropHelper = new WindowInteropHelper(this);
        var hwnd = windowInteropHelper.Handle;

        // Get the devices that can be handled with Raw Input.
        var devices = RawInputDevice.GetDevices();

        // register the keyboard device and you can register device which you need like mouse
        RawInputDevice.RegisterDevice(HidUsageAndPage.Keyboard,
            RawInputDeviceFlags.ExInputSink | RawInputDeviceFlags.NoLegacy, hwnd);

        RawInputDevice.RegisterDevice(HidUsageAndPage.TouchScreen,
            RawInputDeviceFlags.ExInputSink | RawInputDeviceFlags.DevNotify, hwnd);
        RawInputDevice.RegisterDevice(HidUsageAndPage.Pen,
            RawInputDeviceFlags.ExInputSink | RawInputDeviceFlags.DevNotify, hwnd);

        HwndSource source = HwndSource.FromHwnd(hwnd);
        source.AddHook(Hook);
    }

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        const int WM_INPUT = 0x00FF;

        // You can read inputs by processing the WM_INPUT message.
        if (msg == WM_INPUT)
        {
            // Create an RawInputData from the handle stored in lParam.
            var data = RawInputData.FromHandle(lparam);

            // You can identify the source device using Header.DeviceHandle or just Device.
            var sourceDeviceHandle = data.Header.DeviceHandle;
            var sourceDevice = data.Device;

            // The data will be an instance of either RawInputMouseData, RawInputKeyboardData, or RawInputHidData.
            // They contain the raw input data in their properties.
            switch (data)
            {
                case RawInputMouseData mouse:
                    Debug.WriteLine($"Mouse {mouse.Mouse}");
                    break;
                case RawInputKeyboardData keyboard:
                    Debug.WriteLine(keyboard.Keyboard);
                    break;
                case RawInputHidData hid:
                    Debug.WriteLine($"Hid {hid.Hid}");
                    TextBlock.Text = @$"DevicePath: {hid.Device.DevicePath}
VID:{hid.Device.VendorId:X2} PID:{hid.Device.ProductId:X2}
RawData:{hid.Hid}

";
                    if (hid is RawInputDigitizerData rawInputDigitizerData)
                    {
                        foreach (var rawInputDigitizerContact in rawInputDigitizerData.Contacts)
                        {
                            TextBlock.Text += rawInputDigitizerContact.ToString() + "\r\n";
                        }
                    }
                    break;
            }
        }

        return IntPtr.Zero;
    }
}
