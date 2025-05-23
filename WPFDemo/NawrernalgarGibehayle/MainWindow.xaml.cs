﻿using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

using Windows.Win32.Foundation;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Input.Pointer;
using Windows.Win32.UI.WindowsAndMessaging;

using static Windows.Win32.PInvoke;

namespace NawrernalgarGibehayle;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        StylusMove += MainWindow_StylusMove;
    }

    private void MainWindow_StylusMove(object sender, StylusEventArgs e)
    {
        var position = e.GetPosition(this);
        var x = position.X;
        var y = position.Y;
        TouchInfoTextBlock.Text += $"\r\n[WPF StylusMove] Id={e.StylusDevice.Id} XY={x:0.00},{y:0.00}";

        var stylusPointCollection = e.GetStylusPoints(null);
        if (stylusPointCollection.Description.HasProperty(StylusPointProperties.Width))
        {
            var stylusPointPropertyInfo = stylusPointCollection.Description.GetPropertyInfo(StylusPointProperties.Width);
            var width = stylusPointCollection[0].GetPropertyValue(StylusPointProperties.Width);

            TouchInfoTextBlock.Text +=
                $" Width=[Value:{width},Max:{stylusPointPropertyInfo.Maximum},Min:{stylusPointPropertyInfo.Minimum},Resolution:{stylusPointPropertyInfo.Resolution:0.###},Physical:{width/stylusPointPropertyInfo.Resolution:0.###}{stylusPointPropertyInfo.Unit}]";
        }
        if (stylusPointCollection.Description.HasProperty(StylusPointProperties.Height))
        {
            var stylusPointPropertyInfo = stylusPointCollection.Description.GetPropertyInfo(StylusPointProperties.Height);
            var height = stylusPointCollection[0].GetPropertyValue(StylusPointProperties.Height);
            TouchInfoTextBlock.Text +=
                $" Height=[Value:{height},Max:{stylusPointPropertyInfo.Maximum},Min:{stylusPointPropertyInfo.Minimum},Resolution:{stylusPointPropertyInfo.Resolution:0.###},Physical:{height / stylusPointPropertyInfo.Resolution:0.###}{stylusPointPropertyInfo.Unit}]";
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        var windowInteropHelper = new WindowInteropHelper(this);
        var hwnd = windowInteropHelper.Handle;

        HwndSource source = HwndSource.FromHwnd(hwnd)!;
        source.AddHook(Hook);
        }

    private unsafe IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_POINTERUPDATE /*Pointer Update*/)
        {
            Debug.Assert(OperatingSystem.IsWindowsVersionAtLeast(10, 0), "能够收到 WM_Pointer 消息，必定系统版本号不会低");

            var pointerId = (uint) (ToInt32(wParam) & 0xFFFF);
            GetPointerTouchInfo(pointerId, out POINTER_TOUCH_INFO info);
            POINTER_INFO pointerInfo = info.pointerInfo;

            global::Windows.Win32.Foundation.RECT pointerDeviceRect = default;
            global::Windows.Win32.Foundation.RECT displayRect = default;

            GetPointerDeviceRects(pointerInfo.sourceDevice, &pointerDeviceRect, &displayRect);

            uint propertyCount = 0;
            GetPointerDeviceProperties(pointerInfo.sourceDevice, &propertyCount, null);
            POINTER_DEVICE_PROPERTY* pointerDevicePropertyArray =
                stackalloc POINTER_DEVICE_PROPERTY[(int) propertyCount];
            GetPointerDeviceProperties(pointerInfo.sourceDevice, &propertyCount, pointerDevicePropertyArray);
            var pointerDevicePropertySpan =
                new Span<POINTER_DEVICE_PROPERTY>(pointerDevicePropertyArray, (int) propertyCount);

            GetPointerCursorId(pointerId, out uint cursorId);

            var touchInfo = new StringBuilder();
            touchInfo.Append($"[{DateTime.Now}] ");
            touchInfo.AppendLine(
                $"PointerId={pointerId} CursorId={cursorId} PointerDeviceRect={RectToString(pointerDeviceRect)} DisplayRect={RectToString(displayRect)} PropertyCount={propertyCount} SourceDevice={pointerInfo.sourceDevice}");

            var xPropertyIndex = -1;
            var yPropertyIndex = -1;
            var contactIdentifierPropertyIndex = -1;
            var widthPropertyIndex = -1;
            var heightPropertyIndex = -1;

            for (var i = 0; i < pointerDevicePropertySpan.Length; i++)
            {
                POINTER_DEVICE_PROPERTY pointerDeviceProperty = pointerDevicePropertySpan[i];
                var usagePageId = pointerDeviceProperty.usagePageId;
                var usageId = pointerDeviceProperty.usageId;
                // 单位
                var unit = pointerDeviceProperty.unit;
                // 单位指数。 它与 Unit 字段一起定义了设备报告中数据的物理单位。具体来说：
                // - Unit：定义了数据的基本单位，例如厘米、英寸、弧度等。
                // - UnitExponent：表示单位的数量级（即 10 的幂次）。它用于缩放单位值，使其适应不同的范围
                var unitExponent = pointerDeviceProperty.unitExponent;
                touchInfo.Append(
                        $"{UsagePageAndIdConverter.ConvertToString(usagePageId, usageId)} Unit={StylusPointPropertyUnitHelper.FromPointerUnit(unit)}({unit}) UnitExponent={unitExponent}")
                    .Append(
                        $"  LogicalMin={pointerDeviceProperty.logicalMin} LogicalMax={pointerDeviceProperty.logicalMax}")
                    .Append(
                        $"  PhysicalMin={pointerDeviceProperty.physicalMin} PhysicalMax={pointerDeviceProperty.physicalMax}")
                    .AppendLine();

                if (usagePageId == (ushort) HidUsagePage.Generic)
                {
                    if (usageId == (ushort) HidUsage.X)
                    {
                        xPropertyIndex = i;
                    }
                    else if (usageId == (ushort) HidUsage.Y)
                    {
                        yPropertyIndex = i;
                    }
                }
                else if (usagePageId == (ushort) HidUsagePage.Digitizer)
                {
                    if (usageId == (ushort) DigitizersUsageId.Width)
                    {
                        widthPropertyIndex = i;
                    }
                    else if (usageId == (ushort) DigitizersUsageId.Height)
                    {
                        heightPropertyIndex = i;
                    }
                    else if (usageId == (ushort) DigitizersUsageId.ContactIdentifier)
                    {
                        contactIdentifierPropertyIndex = i;
                    }
                }
            }

            var historyCount = pointerInfo.historyCount;
            int[] rawPointerData = new int[propertyCount * historyCount];

            fixed (int* pValue = rawPointerData)
            {
                bool success = GetRawPointerDeviceData(pointerId, historyCount, propertyCount,
                    pointerDevicePropertyArray, pValue);
                Debug.Assert(success);
            }

            var rawPointerPoint = new RawPointerPoint();

            for (int i = 0; i < historyCount; i++)
            {
                var baseIndex = i * propertyCount;

                if (xPropertyIndex >= 0 && yPropertyIndex >= 0)
                {
                    var xValue = rawPointerData[baseIndex + xPropertyIndex];
                    var yValue = rawPointerData[baseIndex + yPropertyIndex];
                    var xProperty = pointerDevicePropertySpan[xPropertyIndex];
                    var yProperty = pointerDevicePropertySpan[yPropertyIndex];

                    // 从 Pointer 算到的只能是屏幕坐标的点，转换进应用程序窗口坐标还需要自己再次计算
                    var xForScreen = ((double) xValue - xProperty.logicalMin) /
                        (xProperty.logicalMax - xProperty.logicalMin) * displayRect.Width;
                    var yForScreen = ((double) yValue - yProperty.logicalMin) /
                        (yProperty.logicalMax - yProperty.logicalMin) * displayRect.Height;

                    rawPointerPoint = rawPointerPoint with
                    {
                        X = xForScreen,
                        Y = yForScreen,
                    };
                }

                if (contactIdentifierPropertyIndex >= 0)
                {
                    // 这里的 Id 关联会出现 id 重复的问题，似乎是在上层处理的
                    var contactIdentifierValue = rawPointerData[baseIndex + contactIdentifierPropertyIndex];

                    rawPointerPoint = rawPointerPoint with
                    {
                        Id = contactIdentifierValue
                    };
                }

                if (widthPropertyIndex >= 0 && heightPropertyIndex >= 0)
                {
                    var widthValue = rawPointerData[baseIndex + widthPropertyIndex];
                    var heightValue = rawPointerData[baseIndex + heightPropertyIndex];

                    var widthProperty = pointerDevicePropertySpan[widthPropertyIndex];
                    var heightProperty = pointerDevicePropertySpan[heightPropertyIndex];

                    // 计算宽度高度的方法：
                    // 1. 计算出宽度 Value 和最大值最小值的比例
                    // 2. 按照比例计算出宽度高度在屏幕上的像素值
                    // 3. 按照比例配合物理最小值和最大值计算出宽度高度的物理值
                    var widthScale = ((double) widthValue - widthProperty.logicalMin) /
                                     (widthProperty.logicalMax - widthProperty.logicalMin);

                    var heightScale = ((double) heightValue - heightProperty.logicalMin) /
                                      (heightProperty.logicalMax - heightProperty.logicalMin);

                    var widthPixel = widthScale * displayRect.Width;
                    var heightPixel = heightScale * displayRect.Height;

                    rawPointerPoint = rawPointerPoint with
                    {
                        RawWidth = widthValue,
                        RawHeight = heightValue,
                        PixelWidth = widthPixel,
                        PixelHeight = heightPixel,
                    };

                    if (StylusPointPropertyUnitHelper.FromPointerUnit(widthProperty.unit) ==
                        StylusPointPropertyUnit.Centimeters)
                    {
                        var unitExponent = (int) widthProperty.unitExponent;

                        // 根据 HID 规范，单位指数的值范围是 0x00-0x0F，带上 mask 可以强行约束范围
                        const byte HidExponentMask = 0x0F;
                        // HID hut1_6.pdf 23.18.4 Generic Unit Exponent
                        // 以下代码也能从 WPF 的 System.Windows.Input.StylusPointer.PointerStylusPointPropertyInfoHelper 找到
                        unitExponent = (byte) (unitExponent & HidExponentMask) switch
                        {
                            5 => 5,
                            6 => 6,
                            7 => 7,
                            8 => -8,
                            9 => -7,
                            0x0A => -6,
                            0x0B => -5,
                            0x0C => -4,
                            0x0D => -3,
                            0x0E => -2,
                            0x0F => -1,
                            _ => unitExponent
                        };
                        // 也可以这么写，正好也是相同的值。只是这么写在玩二进制的转换，不如打一个表好
                        // - unchecked((short) (0xFFF0 | 0xA)) == -6
                        // - unchecked((short) (0xFFF0 | 0x9)) == -7
                        //if (unitExponent > 7)
                        //{
                        //    unitExponent = unchecked((short)(0xFFF0 | unitExponent));
                        //}

                        // 宽度高度都使用相同的单位值好了，预计也没有哪个厂商的触摸框有这么有趣，宽度和高度分别采用不同的单位
                        var exponent = Math.Pow(10, unitExponent);

                        var widthPhysical = widthScale * (widthProperty.physicalMax - widthProperty.physicalMin) *
                                            exponent;
                        var heightPhysical = heightScale * (heightProperty.physicalMax - heightProperty.physicalMin) *
                                             exponent;

                        rawPointerPoint = rawPointerPoint with
                        {
                            // 物理尺寸的计算能够保持和 WPF 的 StylusPoint 拿到的相同
                            PhysicalWidth = widthPhysical,
                            PhysicalHeight = heightPhysical,
                        };
                    }
                }

                if (rawPointerPoint != default)
                {
                    // 默认调试只取一个点好了
                    break;
                }
            }

            touchInfo.AppendLine(
                $"PointerPoint PointerId={pointerInfo.pointerId} XY={pointerInfo.ptPixelLocationRaw.X},{pointerInfo.ptPixelLocationRaw.Y} rc ContactXY={info.rcContactRaw.X},{info.rcContactRaw.Y} ContactWH={info.rcContactRaw.Width},{info.rcContactRaw.Height}");
            touchInfo.AppendLine(
                $"RawPointerPoint Id={rawPointerPoint.Id} XY={rawPointerPoint.X:0.00},{rawPointerPoint.Y:0.00} RawWH={rawPointerPoint.RawWidth},{rawPointerPoint.RawHeight} PixelWH={rawPointerPoint.PixelWidth:0.00},{rawPointerPoint.PixelHeight:0.00} PhysicalWH={rawPointerPoint.PhysicalWidth:0.00},{rawPointerPoint.PhysicalHeight:0.00}cm");

            // 转换为 WPF 坐标系
            var scale = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            // 计算出窗口的左上角坐标对应到屏幕坐标的点
            // 为什么不是在 PointToScreen 传入坐标点，而是传入 0 点呢？这是因为经过了 PointToScreen 方法会丢失精度，即小数点之后的内容会被丢失。因此正常的计算方法都是取 0 点计算出窗口坐标系相对于屏幕坐标系的偏移量
            // 减去偏移量之后，再经过 DPI 缩放即可获取窗口坐标系的坐标
            var originPointToScreen = this.PointToScreen(new Point(0, 0));

            var xWpf = (rawPointerPoint.X + displayRect.left - originPointToScreen.X) / scale;
            var yWpf = (rawPointerPoint.Y + displayRect.top - originPointToScreen.Y) / scale;
            var widthWpf = rawPointerPoint.PixelWidth / scale;
            var heightWpf = rawPointerPoint.PixelHeight / scale;
            touchInfo.AppendLine(
                $"RawPointerPoint For WPF XY={xWpf:0.00},{yWpf:0.00} WH={widthWpf:0.00},{heightWpf:0.00}");

            if (double.IsRealNumber(xWpf) && double.IsRealNumber(yWpf) && double.IsRealNumber(widthWpf) &&
                double.IsRealNumber(heightWpf))
            {
                TouchSizeBorder.Visibility = Visibility.Visible;
                if (TouchSizeBorder.RenderTransform is TranslateTransform translateTransform)
                {
                    translateTransform.X = xWpf - widthWpf / 2;
                    translateTransform.Y = yWpf - heightWpf / 2;
                }

                TouchSizeBorder.Width = widthWpf;
                TouchSizeBorder.Height = heightWpf;
            }

            TouchInfoTextBlock.Text = touchInfo.ToString();
        }

        return 0;

        static string RectToString(global::Windows.Win32.Foundation.RECT rect)
        {
            return $"[XY:{rect.left},{rect.top};WH:{rect.Width},{rect.Height}]";
        }
    }

    private static int ToInt32(WPARAM wParam) => ToInt32((IntPtr) wParam.Value);
    private static int ToInt32(IntPtr ptr) => IntPtr.Size == 4 ? ptr.ToInt32() : (int) (ptr.ToInt64() & 0xffffffff);
}

/// <summary>
///
/// WM_POINTER stack must parse out HID spec usage pages
/// <see cref="http://www.usb.org/developers/hidpage/Hut1_12v2.pdf"/>
/// </summary>
/// Copy from https://github.com/dotnet/wpf
internal enum HidUsagePage : ushort
{
    Undefined = 0x00,
    Generic = 0x01,
    Simulation = 0x02,
    Vr = 0x03,
    Sport = 0x04,
    Game = 0x05,
    Keyboard = 0x07,
    Led = 0x08,
    Button = 0x09,
    Ordinal = 0x0a,
    Telephony = 0x0b,
    Consumer = 0x0c,
    Digitizer = 0x0d,
    Unicode = 0x10,
    Alphanumeric = 0x14,
    BarcodeScanner = 0x8C,
    WeighingDevice = 0x8D,
    MagneticStripeReader = 0x8E,
    CameraControl = 0x90,
    MicrosoftBluetoothHandsfree = 0xfff3,
}

/// <summary>
///
/// 
/// WISP pre-parsed these, WM_POINTER stack must do it itself
/// 
/// See Stylus\biblio.txt - 1
/// <see cref="http://www.usb.org/developers/hidpage/Hut1_12v2.pdf"/> 
/// </summary>
/// Copy from https://github.com/dotnet/wpf
internal enum HidUsage
{
    X = 0x30,
    Y = 0x31,
    Z = 0x32,
    TipPressure = 0x30,
    BarrelPressure = 0x31,
    XTilt = 0x3D,
    YTilt = 0x3E,
    Azimuth = 0x3F,
    Altitude = 0x40,
    Twist = 0x41,
    TipSwitch = 0x42,
    SecondaryTipSwitch = 0x43,
    BarrelSwitch = 0x44,
    TouchConfidence = 0x47,
    Width = 0x48,
    Height = 0x49,
    TransducerSerialNumber = 0x5B,
}

internal static class StylusPointPropertyUnitHelper
{
    // Copy from https://github.com/dotnet/wpf

    /// <summary>
    /// Convert WM_POINTER units to WPF units
    /// </summary>
    /// <param name="pointerUnit"></param>
    /// <returns></returns>
    internal static StylusPointPropertyUnit? FromPointerUnit(uint pointerUnit)
    {
        StylusPointPropertyUnit unit = StylusPointPropertyUnit.None;

        if (_pointerUnitMap.TryGetValue(pointerUnit & UNIT_MASK, out unit))
        {
            return unit;
        }

        return (StylusPointPropertyUnit?) null;
    }

    /// <summary>
    /// Mapping for WM_POINTER based unit, taken from legacy WISP code
    /// </summary>
    private static Dictionary<uint, StylusPointPropertyUnit> _pointerUnitMap =
        new Dictionary<uint, StylusPointPropertyUnit>()
        {
            { 1, StylusPointPropertyUnit.Centimeters },
            { 2, StylusPointPropertyUnit.Radians },
            { 3, StylusPointPropertyUnit.Inches },
            { 4, StylusPointPropertyUnit.Degrees },
        };

    /// <summary>
    /// Mask to extract units from raw WM_POINTER data
    /// <see cref="http://www.usb.org/developers/hidpage/Hut1_12v2.pdf"/> 
    /// </summary>
    private const uint UNIT_MASK = 0x000F;
}
