using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using System.Windows.Threading;
using MSTest.Extensions.Utils;

namespace LeekailawnahelNarjailearyaydi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            MouseDown += MainWindow_MouseDown;
            TouchDown += MainWindow_TouchDown;
            KeyDown += MainWindow_KeyDown;

            var dispatcherTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            dispatcherTimer.Tick += DispatcherTimer_Tick;

            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (CheckTouch())
            {
                StopTouchTextBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                StopTouchTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            StartTouch();
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void MainWindow_TouchDown(object sender, TouchEventArgs e)
        {
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TabletDeviceCollection devices = System.Windows.Input.Tablet.TabletDevices;

            if (devices.Count > 0)
            {
                // Get the Type of InputManager.
                Type inputManagerType = typeof(System.Windows.Input.InputManager);

                // Call the StylusLogic method on the InputManager.Current instance.
                object stylusLogic = inputManagerType.InvokeMember("StylusLogic",
                    BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                    null, InputManager.Current, null);

                if (stylusLogic != null)
                {
                    var wispLogicType = inputManagerType.Assembly.GetType("System.Windows.Input.StylusWisp.WispLogic");

                    var windowInteropHelper = new WindowInteropHelper(this);
                    var hwndSource = HwndSource.FromHwnd(windowInteropHelper.Handle);

                    var unRegisterHwndForInputMethodInfo = wispLogicType.GetMethod("UnRegisterHwndForInput",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                    unRegisterHwndForInputMethodInfo.Invoke(stylusLogic, new object[] {hwndSource});


                    var registerHwndForInputMethodInfo = wispLogicType.GetMethod("RegisterHwndForInput",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                    registerHwndForInputMethodInfo.Invoke(stylusLogic, new object[]
                    {
                        InputManager.Current, PresentationSource.FromVisual(this)
                    });

                    var tabletDevicesType = wispLogicType.GetProperty("TabletDevices",
                        BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic);

                    var getMethod = tabletDevicesType.GetGetMethod(true);

                    var tabletDeviceCollection = (TabletDeviceCollection) getMethod.Invoke(stylusLogic, null);
                    if (tabletDeviceCollection != null && tabletDeviceCollection.Count > 0)
                    {
                        var wispTabletDevice = tabletDeviceCollection[0];
                        var tabletDeviceImpl = wispTabletDevice.GetProperty("TabletDeviceImpl"); // 类型 WispTabletDevice
                        var penThread = tabletDeviceImpl.GetProperty("PenThread");
                        var penThreadWorkder = penThread.GetField("_penThreadWorker");

                        //UpdateScreenMeasurements

                        // 尝试调用 PenContexts 的 Enable 和 Disable 方法
                    }
                }

                //if (stylusLogic != null)
                //{
                //    //  Get the type of the stylusLogic returned from the call to StylusLogic.
                //    Type stylusLogicType = stylusLogic.GetType();

                //    // Loop until there are no more devices to remove.
                //    while (devices.Count > 0)
                //    {
                //        // Remove the first tablet device in the devices collection.
                //        stylusLogicType.InvokeMember("OnTabletRemoved",
                //            BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic,
                //            null, stylusLogic, new object[] { (uint)0 });
                //    }
                //}
            }
        }

        private void ReStartButton_OnClick(object sender, RoutedEventArgs e)
        {
            object stylusLogic = GetStylusLogic();

            if (stylusLogic == null)
            {
                return;
            }

            Type inputManagerType = typeof(System.Windows.Input.InputManager);
            var wispLogicType = inputManagerType.Assembly.GetType("System.Windows.Input.StylusWisp.WispLogic");

            var windowInteropHelper = new WindowInteropHelper(this);
            var hwndSource = HwndSource.FromHwnd(windowInteropHelper.Handle);

            var unRegisterHwndForInputMethodInfo = wispLogicType.GetMethod("UnRegisterHwndForInput",
                BindingFlags.Instance | BindingFlags.NonPublic);

            unRegisterHwndForInputMethodInfo.Invoke(stylusLogic, new object[] {hwndSource});


            var registerHwndForInputMethodInfo = wispLogicType.GetMethod("RegisterHwndForInput",
                BindingFlags.Instance | BindingFlags.NonPublic);

            registerHwndForInputMethodInfo.Invoke(stylusLogic, new object[]
            {
                InputManager.Current,
                PresentationSource.FromVisual(this)
            });
        }

        private void ExitButton_OnClick(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(ExitTouch);
        }

        private void StartTouch()
        {
            object stylusLogic = GetStylusLogic();

            if (stylusLogic == null)
            {
                return;
            }

            Type inputManagerType = typeof(System.Windows.Input.InputManager);
            var wispLogicType = inputManagerType.Assembly.GetType("System.Windows.Input.StylusWisp.WispLogic");

            var registerHwndForInputMethodInfo = wispLogicType.GetMethod("RegisterHwndForInput",
                BindingFlags.Instance | BindingFlags.NonPublic);

            registerHwndForInputMethodInfo.Invoke(stylusLogic, new object[]
            {
                InputManager.Current,
                PresentationSource.FromVisual(this)
            });
        }

        private void ExitTouch()
        {
            TabletDeviceCollection devices = System.Windows.Input.Tablet.TabletDevices;

            if (devices.Count > 0)
            {
                // Get the Type of InputManager.
                Type inputManagerType = typeof(System.Windows.Input.InputManager);

                // Call the StylusLogic method on the InputManager.Current instance.
                object stylusLogic = inputManagerType.InvokeMember("StylusLogic",
                    BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                    null, InputManager.Current, null);

                if (stylusLogic != null)
                {
                    var wispLogicType = inputManagerType.Assembly.GetType("System.Windows.Input.StylusWisp.WispLogic");

                    var windowInteropHelper = new WindowInteropHelper(this);
                    var hwndSource = HwndSource.FromHwnd(windowInteropHelper.Handle);

                    var unRegisterHwndForInputMethodInfo = wispLogicType.GetMethod("UnRegisterHwndForInput",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                    unRegisterHwndForInputMethodInfo.Invoke(stylusLogic, new object[] {hwndSource});
                }
            }
        }

        private object GetStylusLogic()
        {
            TabletDeviceCollection devices = System.Windows.Input.Tablet.TabletDevices;

            if (devices.Count > 0)
            {
                // Get the Type of InputManager.
                Type inputManagerType = typeof(System.Windows.Input.InputManager);

                // Call the StylusLogic method on the InputManager.Current instance.
                object stylusLogic = inputManagerType.InvokeMember("StylusLogic",
                    BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                    null, InputManager.Current, null);

                return stylusLogic;
            }

            return null;
        }

        private static bool CheckTouch()
        {
            bool canTouch = false;

            try
            {
                foreach (TabletDevice device in Tablet.TabletDevices)
                {
                    var deviceProperty = device.GetType().GetProperty("TabletDeviceImpl",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty);
                    var deviceImpl = deviceProperty is null ? device : deviceProperty.GetValue(device);
                    var tabletSize = deviceImpl.GetType().GetProperty("TabletSize",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty);

                    var privateObject = tabletSize?.GetValue(deviceImpl, null);
                    //对于反射调用到的私有属性，请一定判空。即便我们没有修改开发 SDK 的版本号（.NET Framework 4.5），运行时也可能有变化
                    if (privateObject is Size size)
                    {
                        if (device.StylusDevices.Count > 0 && size.Width > 0)
                        {
                            //触摸点大于0 
                            canTouch = true;
                        }
                    }

                    if (canTouch)
                    {
                        break;
                    }
                }
            }
            catch (Exception)
            {
                // 忽略
            }

            return canTouch;
        }
    }
}