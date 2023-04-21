using System.Reflection.Metadata;
using System.Text;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Controls;

using Windows.Win32;
using Microsoft.Win32.SafeHandles;
using UINT32 = System.UInt32;

namespace LikereyearqelJewunejoyel;

internal class Program
{
    [STAThread]
    static unsafe void Main(string[] args)
    {
        var stringBuilder = new StringBuilder();
        UINT32 deviceCount = (UINT32) 0U;
        PInvoke.GetPointerDevices(ref deviceCount, (POINTER_DEVICE_INFO*) IntPtr.Zero);
        POINTER_DEVICE_INFO[] pointerDevices = new POINTER_DEVICE_INFO[(int) (uint) deviceCount];
        fixed (POINTER_DEVICE_INFO* pPointerDevices = pointerDevices)
        {
            PInvoke.GetPointerDevices(ref deviceCount, pPointerDevices);
        }

        foreach (POINTER_DEVICE_INFO pointerDeviceInfo in pointerDevices)
        {
            HANDLE device = pointerDeviceInfo.device;
            UINT32 propertyCount = (UINT32) 0U;

            var deviceSafeHandle = new SafeFileHandle(device.Value, false);
            PInvoke.GetPointerDeviceProperties(deviceSafeHandle, ref propertyCount, (POINTER_DEVICE_PROPERTY*) IntPtr.Zero);

            POINTER_DEVICE_PROPERTY[] pointerProperties = new POINTER_DEVICE_PROPERTY[(int) propertyCount];
            fixed (POINTER_DEVICE_PROPERTY* pPointerProperties = pointerProperties)
            {
                PInvoke.GetPointerDeviceProperties(deviceSafeHandle, ref propertyCount, pPointerProperties);
            }

            stringBuilder.AppendLine("****************************************************");
            stringBuilder.AppendLine(pointerDeviceInfo.productString.ToString());
            foreach (POINTER_DEVICE_PROPERTY prop in pointerProperties)
            {
                stringBuilder.AppendLine("===============================");
                stringBuilder.AppendLine(string.Format("logicalMin   :{0}", (object) prop.logicalMin));
                stringBuilder.AppendLine(string.Format("logicalMax   :{0}", (object) prop.logicalMax));
                stringBuilder.AppendLine(string.Format("physicalMin  :{0}", (object) prop.physicalMin));
                stringBuilder.AppendLine(string.Format("physicalMax  :{0}", (object) prop.physicalMax));
                stringBuilder.AppendLine(string.Format("unit         :{0:X2}", (object) (uint) prop.unit));
                stringBuilder.AppendLine(string.Format("unitExponent :{0:X2}", (object) (uint) prop.unitExponent));
                stringBuilder.AppendLine(string.Format("usagePageId  :{0:X2}", (object) (uint) (ushort) prop.usagePageId));
                stringBuilder.AppendLine(string.Format("usageId      :{0:X2}", (object) (uint) (ushort) prop.usageId));
                ParseProperty(in prop, stringBuilder);
            }
        }

        var application = new Application();
        application.Startup += (sender, eventArgs) =>
        {
            var window = new Window()
            {
                WindowState = WindowState.Maximized
            };

            var textBox = new TextBox()
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                Text = stringBuilder.ToString()
            };
            window.Content = textBox;

            window.StylusDown += (o, downEventArgs) =>
            {
                var stylusPoint = downEventArgs.GetStylusPoints(window);
                var point = stylusPoint.First();

                var pointToScreen = window.PointToScreen(point.ToPoint());
                pointToScreen = point.ToPoint();

                var xStylusPointPropertyInfo = point.Description.GetPropertyInfo(StylusPointProperties.X);
                var yStylusPointPropertyInfo = point.Description.GetPropertyInfo(StylusPointProperties.Y);

                var physicsX = pointToScreen.X / (xStylusPointPropertyInfo.Maximum - xStylusPointPropertyInfo.Minimum) *
                               xStylusPointPropertyInfo.Resolution;

                var physicsY = pointToScreen.Y / (yStylusPointPropertyInfo.Maximum - yStylusPointPropertyInfo.Minimum) *
                               yStylusPointPropertyInfo.Resolution;

                textBox.Text = $"=============\r\nX={point.X};Y={point.Y}\r\n屏幕物理坐标：{physicsX} {xStylusPointPropertyInfo.Unit},{physicsY} {yStylusPointPropertyInfo.Unit}\r\n";

                var dictionary = new Dictionary<Guid, string>
                {
                    { StylusPointProperties.X.Id, "X" },
                    { StylusPointProperties.Y.Id, "Y" },
                    { StylusPointProperties.Width.Id, "Width" },
                    { StylusPointProperties.Height.Id, "Height" },
                    { StylusPointProperties.AltitudeOrientation.Id, "AltitudeOrientation" },
                    { StylusPointProperties.AzimuthOrientation.Id, "AzimuthOrientation" },
                    { StylusPointProperties.PacketStatus.Id, "PacketStatus" },
                    { StylusPointProperties.SystemTouch.Id, "SystemTouch" },
                    { StylusPointProperties.NormalPressure.Id, "NormalPressure" },
                    { Guid.Parse("02585b91-049b-4750-9615-df8948ab3c9c"), "DEVICE_CONTACT" },
                };

                foreach (var stylusPointPropertyInfo in point.Description.GetStylusPointProperties())
                {
                    if (!dictionary.TryGetValue(stylusPointPropertyInfo.Id, out var idName))
                    {
                        idName = stylusPointPropertyInfo.Id.ToString();
                    }

                    textBox.Text += $"Minimum={stylusPointPropertyInfo.Minimum} Maximum={stylusPointPropertyInfo.Maximum} Resolution={stylusPointPropertyInfo.Resolution} Unit={stylusPointPropertyInfo.Unit} Id={idName}\r\n";
                }
            };

            window.Show();
        };
        application.Run();
    }

    private static void ParseProperty(in POINTER_DEVICE_PROPERTY prop, StringBuilder stringBuilder)
    {
        string usagePage = string.Empty;
        string name = string.Empty;
        if (prop.usagePageId == 0x01) //UsagePage(Generic Desktop[1]) 
        {
            usagePage = "Generic Desktop";

            name = prop.usageId switch
            {
                0x30 => "X",
                0x31 => "Y",
                0x32 => "Z",

                0x33 => "Rx",
                0x34 => "Ry",
                0x35 => "Rz",

                _ => "Other"
            };
        }

        if (prop.usagePageId == 0x0D) // Digitizers Page
        {
            usagePage = "Digitizers Page";

            name = prop.usageId switch
            {
                0x51 => $"Contact Identifier; Contact Count（Maximum）：{prop.logicalMax}",
                0x42 => "Tip Switch",

                _ => "Other"
            };
        }

        stringBuilder.AppendLine($"{usagePage} Name={name}");
    }
}
