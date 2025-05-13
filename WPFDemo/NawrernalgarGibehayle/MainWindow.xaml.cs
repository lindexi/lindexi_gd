using System.Diagnostics;
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
        TouchInfoTextBlock.Text = $"\r\n[WPF StylusMove] Id={e.StylusDevice.Id} XY={x:0.00},{y:0.00}";
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        var windowInteropHelper = new WindowInteropHelper(this);
        var hwnd = windowInteropHelper.Handle;

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0))
        {
            return;
        }

        // 这里用 SetWindowLongPtrW 的原因是，64位的程序调用 32位的 SetWindowLongW 会导致异常，第三位参数不匹配方法指针，详细请看
        // [实战经验：SetWindowLongPtr在开发64位程序的使用方法 | 官方博客 | 拓扑梅尔智慧办公平台 | TopomelBox 官方站点](https://www.topomel.com/archives/245.html )

        _newWndProc = Hook;
        var functionPointer = Marshal.GetFunctionPointerForDelegate(_newWndProc);

        if (Environment.Is64BitProcess)
        {
            _oldWndProc = SetWindowLongPtrW(hwnd, (int) WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC, functionPointer);
        }
        else
        {
            _oldWndProc = GetWindowLong(new HWND(hwnd), WINDOW_LONG_PTR_INDEX.GWL_WNDPROC);
            SetWindowLong(new HWND(hwnd), WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, functionPointer.ToInt32());
        }
    }

    [DllImport("User32.dll")]
    private static extern IntPtr SetWindowLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    // cswin32 生成的是 [MarshalAs(UnmanagedType.FunctionPtr)] winmdroot.UI.WindowsAndMessaging.WNDPROC lpPrevWndFunc 的参数。咱这里已经拿到了函数指针，所以不能使用 WNDPROC 委托
    [DllImport("USER32.dll", ExactSpelling = true, EntryPoint = "CallWindowProcW"),
     DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("windows5.0")]
    private static extern LRESULT CallWindowProc(nint lpPrevWndFunc, HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam);

    private WNDPROC? _newWndProc;
    private IntPtr _oldWndProc;

    [SupportedOSPlatform("windows5.0")]
    private unsafe LRESULT Hook(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == WM_POINTERUPDATE /*Pointer Update*/)
        {
            var result = CallWindowProc(_oldWndProc, hwnd, msg, wParam, lParam);

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

                    var widthScale = ((double) widthValue - widthProperty.logicalMin) /
                                     (widthProperty.logicalMax - widthProperty.logicalMin);

                    var heightScale = ((double) heightValue - heightProperty.logicalMin) /
                                      (heightProperty.logicalMax - heightProperty.logicalMin);

                    var widthPixel = widthScale * displayRect.Width;
                    var heightPixel = heightScale * displayRect.Height;

                    rawPointerPoint = rawPointerPoint with
                    {
                        PixelWidth = widthPixel,
                        PixelHeight = heightPixel,
                    };

                    if (StylusPointPropertyUnitHelper.FromPointerUnit(widthProperty.unit) ==
                        StylusPointPropertyUnit.Centimeters)
                    {
                        var unitExponent = (int) widthProperty.unitExponent;
                        if (unitExponent < -8 || unitExponent > 7)
                        {
                            unitExponent = -2;
                        }

                        var widthPhysical = widthScale * (widthProperty.physicalMax - widthProperty.physicalMin) *
                                            Math.Pow(10, unitExponent);
                        var heightPhysical = heightScale * (heightProperty.physicalMax - heightProperty.physicalMin) *
                                             Math.Pow(10, unitExponent);

                        rawPointerPoint = rawPointerPoint with
                        {
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
                $"RawPointerPoint Id={rawPointerPoint.Id} XY={rawPointerPoint.X:0.00},{rawPointerPoint.Y:0.00} PixelWH={rawPointerPoint.PixelWidth:0.00},{rawPointerPoint.PixelHeight:0.00} PhysicalWH={rawPointerPoint.PhysicalWidth:0.00},{rawPointerPoint.PhysicalHeight:0.00}cm");

            // 转换为 WPF 坐标系
            var scale = VisualTreeHelper.GetDpi(this).PixelsPerDip;
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

            TouchInfoTextBlock.Text += "\r\n" + touchInfo.ToString();

            return result;
        }

        return CallWindowProc(_oldWndProc, hwnd, msg, wParam, lParam);

        static string RectToWHString(global::Windows.Win32.Foundation.RECT rect)
        {
            return $"[WH:{rect.Width},{rect.Height}]";
        }

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

enum StylusPointPropertyUnit
{
    None,
    Centimeters,
    Radians,
    Inches,
    Degrees,
}