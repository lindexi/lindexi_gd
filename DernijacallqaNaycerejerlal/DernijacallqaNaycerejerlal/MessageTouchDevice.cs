using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace DernijacallqaNaycerejerlal
{
    public class MessageTouchDevice : TouchDevice
    {
        /// <inheritdoc />
        private MessageTouchDevice(int deviceId, Window window) : base(deviceId)
        {
            Window = window;
        }

        /// <summary>
        /// 使用消息触摸
        /// 注意 开启了消息触摸之后，原有的 WPF 触摸将会无法再次使用
        /// </summary>
        public static void UseMessageTouch(Window window)
        {
            if (ReferenceEquals(window, null)) throw new ArgumentNullException(nameof(window));
            var hWnd = new WindowInteropHelper(window).Handle;

            if (hWnd == IntPtr.Zero)
            {
                throw new InvalidOperationException("请在SourceInitialized之后调用这个方法");
            }

            // 先禁用 WPF 触摸
            if (!TabletHelper.HasRemovedDevices)
            {
                TabletHelper.DisableWPFTabletSupport(hWnd);
            }

            NativeMethods.RegisterTouchWindow(hWnd, NativeMethods.TWF_WANTPALM);
            HwndSource source = HwndSource.FromHwnd(hWnd);
            Debug.Assert(source != null);

            source.AddHook((IntPtr hwnd, int msg, IntPtr param, IntPtr lParam, ref bool handled) =>
            {
                WndProc(window, msg, param, lParam, ref handled);
                return IntPtr.Zero;
            });
        }


        /// <inheritdoc />
        public override TouchPoint GetTouchPoint(IInputElement relativeTo)
        {
            return new TouchPoint(this, Position, new Rect(Position, Size), TouchAction);
        }

        /// <inheritdoc />
        public override TouchPointCollection GetIntermediateTouchPoints(IInputElement relativeTo)
        {
            return new TouchPointCollection()
            {
                GetTouchPoint(relativeTo)
            };
        }

        private static readonly Dictionary<int, MessageTouchDevice>
            _devices = new Dictionary<int, MessageTouchDevice>();

        /// <summary>
        /// 触摸点
        /// </summary>
        private Point Position { set; get; }

        /// <summary>
        /// 触摸大小
        /// </summary>
        private Size Size { set; get; }

        private Window Window { get; }

        private TouchAction TouchAction { set; get; }

        private void Down()
        {
            TouchAction = TouchAction.Down;

            if (!IsActive)
            {
                SetActiveSource(PresentationSource.FromVisual(Window));

                Activate();
                ReportDown();
            }
            else
            {
                ReportDown();
            }
        }

        private void Move()
        {
            TouchAction = TouchAction.Move;

            ReportMove();
        }

        private void Up()
        {
            TouchAction = TouchAction.Up;

            ReportUp();
            Deactivate();

            _devices.Remove(Id);
        }

        private static void WndProc(Window window, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_TOUCH)
            {
                var inputCount = wParam.ToInt32() & 0xffff;
                var inputs = new NativeMethods.TOUCHINPUT[inputCount];

                if (NativeMethods.GetTouchInputInfo(lParam, inputCount, inputs, NativeMethods.TouchInputSize))
                {
                    for (int i = 0; i < inputCount; i++)
                    {
                        var input = inputs[i];
                        // 下面没有处理 DPI 问题
                        // 相对的是没有处理 DPI 的屏幕坐标
                        // 因为是 物理屏幕坐标的像素的百分之一表示，需要除 100 计算像素
                        var position = new Point(input.X / 100.0, input.Y / 100.0);
                        var size = new Size(input.CxContact / 100.0, input.CyContact / 100.0);

                        if (!_devices.TryGetValue(input.DwID, out var device))
                        {
                            device = new MessageTouchDevice(input.DwID, window);
                            _devices.Add(input.DwID, device);
                        }

                        if (!device.IsActive && input.DwFlags.HasFlag(NativeMethods.TOUCHEVENTF.TOUCHEVENTF_DOWN))
                        {
                            device.Position = position;
                            device.Size = size;
                            device.Down();
                        }
                        else if (device.IsActive && input.DwFlags.HasFlag(NativeMethods.TOUCHEVENTF.TOUCHEVENTF_UP))
                        {
                            device.Position = position;
                            device.Size = size;
                            device.Up();
                        }
                        else if (device.IsActive && input.DwFlags.HasFlag(NativeMethods.TOUCHEVENTF.TOUCHEVENTF_MOVE))
                        {
                            device.Position = position;
                            device.Size = size;
                            device.Move();
                        }
                    }
                }

                NativeMethods.CloseTouchInputHandle(lParam);
                handled = true;
            }
        }
    }
}