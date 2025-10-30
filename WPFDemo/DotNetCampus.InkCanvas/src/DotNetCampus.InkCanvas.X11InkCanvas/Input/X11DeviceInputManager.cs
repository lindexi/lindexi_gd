// Copyright 1996-1997 by Frederic Lepied, France. <Frederic.Lepied@sugix.frmug.org>
//
// Permission to use, copy, modify, distribute, and sell this software and its
// documentation for any purpose is  hereby granted without fee, provided that
// the  above copyright   notice appear  in   all  copies and  that both  that
// copyright  notice   and   this  permission   notice  appear  in  supporting
// documentation, and that   the  name of  the authors   not  be  used  in
// advertising or publicity pertaining to distribution of the software without
// specific,  written      prior  permission.     The authors  make  no
// representations about the suitability of this software for any purpose.  It
// is provided "as is" without express or implied warranty.
//
// THE AUTHORS  DISCLAIMS ALL   WARRANTIES WITH REGARD  TO  THIS SOFTWARE,
// INCLUDING ALL IMPLIED   WARRANTIES OF MERCHANTABILITY  AND   FITNESS, IN NO
// EVENT  SHALL THE AUTHORS  BE   LIABLE   FOR ANY  SPECIAL, INDIRECT   OR
// CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE,
// DATA  OR PROFITS, WHETHER  IN  AN ACTION OF  CONTRACT,  NEGLIGENCE OR OTHER
// TORTIOUS  ACTION, ARISING    OUT OF OR   IN  CONNECTION  WITH THE USE    OR
// PERFORMANCE OF THIS SOFTWARE.
//
// Copyright © 2007 Peter Hutterer
// Copyright © 2009 Red Hat, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice (including the next
// paragraph) shall be included in all copies or substantial portions of the
// Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

// The MIT License (MIT)
//
// Copyright (c) .NET Foundation and Contributors
// All Rights Reserved
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// https://gitlab.freedesktop.org/xorg/app/xinput/-/blob/f550dfbb347ec62ca59e86f79a9e4fe43417ab39/src/xinput.c
// https://gitlab.freedesktop.org/xorg/app/xinput/-/blob/f550dfbb347ec62ca59e86f79a9e4fe43417ab39/src/test_xi2.c
// https://github.com/AvaloniaUI/Avalonia/blob/e0127c610c38701c3af34f580273f6efd78285b5/src/Avalonia.X11/XI2Manager.cs

// 虽然我有真的触摸框，用不着
// Thanks to the amazing Peter Hutterer and Martin Kepplinger for creating evemu recordings
// for touchscreens
// https://github.com/whot/evemu-devices

using System.Text;
using DotNetCampus.Logging;
using UnoInk.Inking.X11Platforms;
using X11ApplicationFramework.Apps;
using X11ApplicationFramework.Natives;
using Point2D = DotNetCampus.Numerics.Geometry.Point2D;
using static X11ApplicationFramework.Natives.XLib;

namespace DotNetCampus.InkCanvas.X11InkCanvas.Input;

unsafe class X11DeviceInputManager
{
    /// <summary>
    /// 设备输入信息管理器
    /// </summary>
    /// <param name="infoManager"></param>
    /// <param name="x11WindowIntPtr">关联的窗口，如果收到的数据不是发给此窗口的，则不应该在此进行处理</param>
    public X11DeviceInputManager(X11InfoManager infoManager, nint x11WindowIntPtr)
    {
        X11WindowIntPtr = x11WindowIntPtr;
        _infoManager = infoManager;

        var devices = (XIDeviceInfo*) XLib.XIQueryDevice(Display,
            (int) XiPredefinedDeviceId.XIAllMasterDevices, out int num);
        Log.Info($"[InkCore][X11Apps] DeviceNumber={num}");
        XIDeviceInfo? pointerDevice = default;
        for (var c = 0; c < num; c++)
        {
            Log.Info($"[InkCore][X11Apps] XIDeviceInfo [{c}] Deviceid={devices[c].Deviceid} {devices[c].Use}");

            if (devices[c].Use == XiDeviceType.XIMasterPointer
                // 调试下，不是选首个就结束循环，因此这里的判断是有效的
                && pointerDevice == null)
            {
                pointerDevice = devices[c];
#if DEBUG
                // 调试下，来一次遍历吧
                continue;
#else
                break;
#endif
            }
        }

        PointerDevice = pointerDevice;

        // ABS_MT_TOUCH_MAJOR ABS_MT_TOUCH_MINOR
        // https://www.kernel.org/doc/html/latest/input/multi-touch-protocol.html
        _touchMajorAtom = XLib.XInternAtom(Display, "Abs MT Touch Major", false);
        _touchMinorAtom = XLib.XInternAtom(Display, "Abs MT Touch Minor", false);
        _pressureAtom = XLib.XInternAtom(Display, "Abs MT Pressure", false);

        Log.Info($"[InkCore][X11Apps] ABS_MT_TOUCH_MAJOR={_touchMajorAtom} Name={XLib.GetAtomName(Display, _touchMajorAtom)} ABS_MT_TOUCH_MINOR={_touchMinorAtom} Name={XLib.GetAtomName(Display, _touchMinorAtom)} Abs_MT_Pressure={_pressureAtom} Name={XLib.GetAtomName(Display, _pressureAtom)}");

        if (pointerDevice != null)
        {
            Log.Info($"[InkCore][X11Apps] pointerDevice.Value.NumClasses={pointerDevice.Value.NumClasses}");

            UpdateValuators(pointerDevice.Value.Classes, pointerDevice.Value.NumClasses);
        }

        if (TouchMajorValuatorClassInfo is null && TouchMinorValuatorClassInfo is null)
        {
            LogAllValuatorsMissTouchSize();
        }

        if (PressureValuatorClassInfo is null)
        {
            Log.Warn($"[InkCore][X11Apps] 读取不到压感信息 PressureValuatorClassInfo is null");
        }
    }

    /// <summary>
    /// 所关联的窗口信息
    /// </summary>
    public IntPtr X11WindowIntPtr { get; }

    /// <summary>
    /// 关联的屏幕的物理宽度
    /// </summary>
    public double? AssociatedMonitorPhysicalWidthCentimetre { get; set; }

    /// <summary>
    /// 关联的屏幕的物理高度
    /// </summary>
    public double? AssociatedMonitorPhysicalHeightCentimetre { get; set; }

    /// <summary>
    /// 关联的屏幕的像素宽度
    /// </summary>
    public double? AssociatedMonitorPixelWidth { get; set; }

    /// <summary>
    /// 关联的屏幕的像素高度
    /// </summary>
    public double? AssociatedMonitorPixelHeight { get; set; }

    /// <summary>
    /// 更新 Valuator 信息
    /// </summary>
    /// <param name="classes"></param>
    /// <param name="num"></param>
    /// https://github.com/AvaloniaUI/Avalonia/pull/17321
    private void UpdateValuators(XIAnyClassInfo** classes, int num)
    {
        _valuators.Clear();
        _scrollers.Clear();

        for (int i = 0; i < num; i++)
        {
            var xiAnyClassInfo = classes[i];
            if (xiAnyClassInfo->Type == XiDeviceClass.XIValuatorClass)
            {
                _valuators.Add(*((XIValuatorClassInfo**) classes)[i]);
            }
            else if (xiAnyClassInfo->Type == XiDeviceClass.XIScrollClass)
            {
                _scrollers.Add(*((XIScrollClassInfo**) classes)[i]);
            }
        }

        TouchMajorValuatorClassInfo = null;
        TouchMinorValuatorClassInfo = null;
        PressureValuatorClassInfo = null;

        for (var i = 0; i < _valuators.Count; i++)
        {
            var xiValuatorClassInfo = _valuators[i];

            if (xiValuatorClassInfo.Label == _touchMajorAtom)
            {
                TouchMajorValuatorClassInfo = xiValuatorClassInfo;
            }
            else if (xiValuatorClassInfo.Label == _touchMinorAtom)
            {
                TouchMinorValuatorClassInfo = xiValuatorClassInfo;
            }
            else if (xiValuatorClassInfo.Label == _pressureAtom)
            {
                PressureValuatorClassInfo = xiValuatorClassInfo;
            }
        }

        if (TouchMajorValuatorClassInfo is null && TouchMinorValuatorClassInfo is null)
        {
            LogAllValuatorsMissTouchSize();
        }
        else
        {
            Log.Info($"[InkCore][X11Apps][X11DeviceInputManager][Valuator] TouchMajorValuatorClassInfo: {(TouchMajorValuatorClassInfo is null ? "null" : GetValuatorInfo(TouchMajorValuatorClassInfo.Value))}; TouchMinorValuatorClassInfo:{(TouchMinorValuatorClassInfo is null ? "null" : GetValuatorInfo(TouchMinorValuatorClassInfo.Value))}");
        }
    }

    /// <summary>
    /// 读取不到触摸宽度高度信息时进行记录日志
    /// </summary>
    private void LogAllValuatorsMissTouchSize()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"[InkCore][X11Apps] 读取不到触摸宽度高度信息 TouchMajorValuatorClassInfo is null && TouchMinorValuatorClassInfo is null\r\n当前能读取到的模式列表如下:");

        for (var i = 0; i < _valuators.Count; i++)
        {
            XIValuatorClassInfo xiValuatorClassInfo = _valuators[i];

            stringBuilder.AppendLine($"[Valuator] [{i}/{_valuators.Count}] {GetValuatorInfo(xiValuatorClassInfo)}");
        }

        var message = stringBuilder.ToString();
        Log.Info(message); // 两边信息记录，方便在 Info 日志里面看到直接的内容，可以方便关联上下日志
        Log.Warn(message);
    }

    private string GetValuatorInfo(XIValuatorClassInfo xiValuatorClassInfo)
    {
        var label = xiValuatorClassInfo.Label;
        var name = GetAtomName(Display, label);
        return $"[{name}({label})] Label={label} Type={xiValuatorClassInfo.Type} Sourceid={xiValuatorClassInfo.Sourceid} Number={xiValuatorClassInfo.Number} Min={xiValuatorClassInfo.Min} Max={xiValuatorClassInfo.Max} Value={xiValuatorClassInfo.Value} Resolution={xiValuatorClassInfo.Resolution} Mode={xiValuatorClassInfo.Mode}";
    }

    private readonly X11InfoManager _infoManager;
    private IntPtr Display => _infoManager.Display;

    public XIDeviceInfo? PointerDevice { get; }

    public XIValuatorClassInfo? TouchMajorValuatorClassInfo { get; private set; }

    public XIValuatorClassInfo? TouchMinorValuatorClassInfo { get; private set; }

    public XIValuatorClassInfo? PressureValuatorClassInfo { get; private set; }

    public IReadOnlyList<XIValuatorClassInfo> Valuators => _valuators;
    public IReadOnlyList<XIScrollClassInfo> Scrollers => _scrollers;

    private readonly List<XIValuatorClassInfo> _valuators = new List<XIValuatorClassInfo>();
    private readonly List<XIScrollClassInfo> _scrollers = new List<XIScrollClassInfo>();

    /// <summary>
    /// 调度消息处理
    /// </summary>
    /// <param name="xiEvent"></param>
    /// <returns>返回 false 代表消息不能被处理</returns>
    public void DispatchMessage(XIDeviceEvent* xiEvent)
    {
        var (shouldIgnore, deviceInputArgs) = ParseDeviceInputArgs(xiEvent);
        if (shouldIgnore)
        {
            // 比如这是模拟的鼠标消息
            return;
        }

        if (xiEvent->evtype is XiEventType.XI_TouchBegin or XiEventType.XI_ButtonPress)
        {
            OnDown(in deviceInputArgs);
        }
        else if (xiEvent->evtype is XiEventType.XI_TouchUpdate or XiEventType.XI_Motion)
        {
            //Console.WriteLine($"Move={id} {stylusPoint.Point.X},{stylusPoint.Point.Y}");
            OnMove(in deviceInputArgs);
        }
        else if (xiEvent->evtype is XiEventType.XI_TouchEnd or XiEventType.XI_ButtonRelease)
        {
            OnUp(in deviceInputArgs);
        }
        //// 其他杂项
        // 独立在 HandleDeviceChanged 方法。原因是不能经过 ParseDeviceInputArgs 转换
        //else if(xiEvent->evtype is XiEventType.XI_DeviceChanged)
        //{
        //    // 设备改变，需要更新设备信息
        //    var changed = (XIDeviceChangedEvent*) xiEvent;
        //}
    }

    public void HandleDeviceChanged(XIDeviceEvent* xiEvent)
    {
        if (xiEvent->evtype is not XiEventType.XI_DeviceChanged)
        {
            throw new ArgumentException($"Expected EventType is XI_DeviceChanged. Actual EventType is {xiEvent->evtype}");
        }

        var changed = (XIDeviceChangedEvent*) xiEvent;
        HandleDeviceChanged(changed);
    }

    public void HandleDeviceChanged(XIDeviceChangedEvent* changed)
    {
        Log.Info($"[X11DeviceInputManager] XI_DeviceChanged Deviceid={changed->Deviceid} Sourceid={changed->Sourceid} CurrentPointerDevice={PointerDevice?.Deviceid ?? -1} 设备改变，需要更新设备信息");

        UpdateValuators(changed->Classes, changed->NumClasses);
    }

    private (bool ShouldIgnore, DeviceInputArgs Args) ParseDeviceInputArgs(XIDeviceEvent* xiDeviceEvent)
    {
        bool isMouse = false;

        if (xiDeviceEvent->evtype is
            XiEventType.XI_ButtonPress
            or XiEventType.XI_ButtonRelease
            or XiEventType.XI_Motion)
        {
            if ((xiDeviceEvent->flags & XiDeviceEventFlags.XIPointerEmulated) ==
                XiDeviceEventFlags.XIPointerEmulated)
            {
                // 多指触摸下是模拟的，则忽略
                //Console.WriteLine("多指触摸下是模拟的");
                return (ShouldIgnore: true, Args: default);
            }

            isMouse = true;
        }

        var timestamp = (ulong) xiDeviceEvent->time.ToInt64();
        var state = (XModifierMask) xiDeviceEvent->mods.Effective;

        var valuatorDictionary = _cacheValuatorDictionary;
        valuatorDictionary.Clear();

        var values = xiDeviceEvent->valuators.Values;
        var bitMaskLength = xiDeviceEvent->valuators.MaskLen * 8;

        // [X11DeviceInputManager] ParseDeviceInputArgs bitMaskLength=537006144 Mask=0
        // Unhandled exception. System.NullReferenceException: Object reference not set to an instance of an object.
        //StaticDebugLogger.WriteLine($"[X11DeviceInputManager] ParseDeviceInputArgs bitMaskLength={bitMaskLength} Mask={new IntPtr(xiDeviceEvent->valuators.Mask)}");
        var hasMask = new IntPtr(xiDeviceEvent->valuators.Mask) != 0;

        for (var c = 0; c < bitMaskLength && hasMask; c++)
        {
            if (XIMaskIsSet(xiDeviceEvent->valuators.Mask, c))
            {
                valuatorDictionary[c] = *values;
                values++;
            }
        }

        float? pressure = null;

        double? physicalWidth = null;
        double? physicalHeight = null;

        double? pixelWidth = null;
        double? pixelHeight = null;

        var touchMajorValuatorClassInfo = TouchMajorValuatorClassInfo;
        var touchMinorValuatorClassInfo = TouchMinorValuatorClassInfo;
        //var valuators = Valuators;

        foreach (var (key, value) in valuatorDictionary)
        {
            if (key == touchMajorValuatorClassInfo?.Number)
            {
                var screenPhysicalWidthCentimetre = AssociatedMonitorPhysicalWidthCentimetre ?? _infoManager.ScreenPhysicalWidthCentimetre;
                var screenPixelWidth = AssociatedMonitorPixelWidth ?? _infoManager.XDisplayWidth;

                var ratio = (value - touchMajorValuatorClassInfo.Value.Min) /
                            (touchMajorValuatorClassInfo.Value.Max -
                             touchMajorValuatorClassInfo.Value.Min);

                physicalWidth = ratio * screenPhysicalWidthCentimetre;
                pixelWidth = ratio * screenPixelWidth;
            }
            else if (key == touchMinorValuatorClassInfo?.Number)
            {
                var screenPhysicalHeightCentimetre = AssociatedMonitorPhysicalHeightCentimetre ?? _infoManager.ScreenPhysicalHeightCentimetre;
                var screenPixelHeight = AssociatedMonitorPixelHeight ?? _infoManager.XDisplayHeight;

                var ratio = (value - touchMinorValuatorClassInfo.Value.Min) /
                               (touchMinorValuatorClassInfo.Value.Max -
                                touchMinorValuatorClassInfo.Value.Min);

                physicalHeight = ratio * screenPhysicalHeightCentimetre;
                pixelHeight = ratio * screenPixelHeight;
            }
            else if (key == PressureValuatorClassInfo?.Number)
            {
                var xiValuatorClassInfo = PressureValuatorClassInfo.Value;

                pressure = (float) ((value - xiValuatorClassInfo.Min) / (xiValuatorClassInfo.Max - xiValuatorClassInfo.Min));
            }
        }

        var id = xiDeviceEvent->detail;
        if (isMouse)
        {
            // 由于在 XI_ButtonPress 时的 id 是 1 而 XI_Motion 是 0 导致无法画出线
            id = 0;
        }

        var deviceInputPoint = new DeviceInputPoint(new Point2D(xiDeviceEvent->event_x, xiDeviceEvent->event_y), timestamp)
        {
            Pressure = pressure,
            PhysicalWidth = physicalWidth,
            PhysicalHeight = physicalHeight,

            PixelWidth = pixelWidth,
            PixelHeight = pixelHeight
        };


        StaticDebugLogger.WriteLine($"[X11DeviceInputManager][{xiDeviceEvent->evtype}][{id}] {deviceInputPoint.Position.X:0.00},{deviceInputPoint.Position.Y:0.00} WH:{physicalWidth:0.00},{physicalHeight:0.00} valuator:{valuatorDictionary.Count}");

        var deviceInputArgs = new DeviceInputArgs(id, isMouse, deviceInputPoint);
        return (false, deviceInputArgs);
    }

    private void OnDown(in DeviceInputArgs deviceInputArgs)
    {
        DevicePressed?.Invoke(this, deviceInputArgs);
    }

    private void OnMove(in DeviceInputArgs deviceInputArgs)
    {
        DeviceMoved?.Invoke(this, deviceInputArgs);
    }

    private void OnUp(in DeviceInputArgs deviceInputArgs)
    {
        DeviceReleased?.Invoke(this, deviceInputArgs);
    }

    public event EventHandler<DeviceInputArgs>? DevicePressed;
    public event EventHandler<DeviceInputArgs>? DeviceMoved;
    public event EventHandler<DeviceInputArgs>? DeviceReleased;

    private readonly Dictionary<int, double> _cacheValuatorDictionary = new Dictionary<int, double>();
    private readonly IntPtr _touchMajorAtom;
    private readonly IntPtr _touchMinorAtom;
    private readonly IntPtr _pressureAtom;
    public List<DeviceInputPoint> CurrentCacheDeviceInputPointList { get; } = new List<DeviceInputPoint>();

    /// <summary>
    /// 消息的版本，如果超过版本，则缓存失效
    /// </summary>
    /// 仅仅是为了解决缓存问题，解决每次触摸都会创建对象导致 GC 压力
    /// 用于 <see cref="CurrentCacheDeviceInputPointList"/> 是否非当前数据
    public uint MessageVersion { get; private set; }

    /// <summary>
    /// 尝试读取消息队列里面的内容，将相同的触摸事件压到一起，用于提升性能
    /// </summary>
    public void TryReadEvents()
    {
        CurrentCacheDeviceInputPointList.Clear();
        MessageVersion++;

        DeviceInputArgs lastArgs = default;

        ReadEventInner();
        // 再执行一次，防止还有最后的点没有调度
        DispatchArgsList();


        void ReadEventInner()
        {
            var count = XEventsQueued(Display, 0 /*QueuedAlready*/);
            if (count > 1)
            {
                // 这个 count 越大证明越慢
                StaticDebugLogger.WriteLine($"卡顿度 XEventsQueued={count}");
            }

            if (count == 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                MessageVersion++;

                XPeekEvent(Display, out var @event);

                if (@event.AnyEvent.window != X11WindowIntPtr)
                {
                    StaticDebugLogger.WriteLine($"[X11DeviceInputManager] TryReadEvents 读取结束，因为读取到非当前关联窗口的消息。 AnyEvent.window={@event.AnyEvent.window} X11WindowIntPtr={X11WindowIntPtr}");

                    return;
                }

                if (@event.type == XEventName.GenericEvent)
                {
                    void* data = &@event.GenericEventCookie;
                    XGetEventData(Display, data);

                    try
                    {
                        var xiEvent = (XIEvent*) @event.GenericEventCookie.data;
                        if (xiEvent->evtype is
                            // 只有移动系的，才可以合并，其他的不能合并
                            //XiEventType.XI_ButtonPress
                            //or XiEventType.XI_ButtonRelease
                            XiEventType.XI_Motion
                            //or XiEventType.XI_TouchBegin
                            or XiEventType.XI_TouchUpdate
                           //or XiEventType.XI_TouchEnd
                           )
                        {
                            var xiDeviceEvent = (XIDeviceEvent*) xiEvent;

                            //if(xiDeviceEvent->EventWindow!=)

                            var (shouldIgnore, args) = ParseDeviceInputArgs(xiDeviceEvent);
                            if (!shouldIgnore)
                            {
                                HandleEvent(in args);
                            }

                            // 读走数据，用于下次读取到新的数据
                            XNextEvent(Display, out _);

                            // 使用 continue 重新进入循环
                            continue;
                        }
                    }
                    finally
                    {
                        XFreeEventData(Display, data);
                    }
                }

                // 这些都是非触摸的，不能处理，返回给到外面
                return;
            }
        }

        void HandleEvent(in DeviceInputArgs args)
        {
            if (CurrentCacheDeviceInputPointList.Count == 0)
            {
                CurrentCacheDeviceInputPointList.Add(args.Point);
                lastArgs = args;
            }
            else
            {
                var currentId = lastArgs.Id;
                var currentIsMouse = lastArgs.IsMouse;

                var isSame = args.Id == currentId
                             // 理论上这是不需要的，只是担心可能确实存在 Id 是 0 的点的情况
                             && args.IsMouse == currentIsMouse;

                if (isSame)
                {
                    CurrentCacheDeviceInputPointList.Add(args.Point);
                    lastArgs = args;
                }
                else
                {
                    // 不相同，这是其他点了
                    DispatchArgsList();
                    // 重新开始
                    CurrentCacheDeviceInputPointList.Clear();
                    CurrentCacheDeviceInputPointList.Add(args.Point);
                    lastArgs = args;
                }
            }
        }

        void DispatchArgsList()
        {
            if (CurrentCacheDeviceInputPointList.Count == 1)
            {
                // 只有 1 个事件，则直接调度即可
                var deviceInputArgs = lastArgs;
                OnMove(in deviceInputArgs);
            }
            else if (CurrentCacheDeviceInputPointList.Count > 1)
            {
                // 有多个事件，则取当前最后的事件为主
                var lastDeviceInputArgs = lastArgs;
                lastDeviceInputArgs = lastDeviceInputArgs with
                {
                    // 设置当前以及消息版本，用于直接使用 CurrentCacheDeviceInputPointList 缓存，减少每次都创建
                    InputManager = this,
                    MessageVersion = MessageVersion,
                };
                OnMove(in lastDeviceInputArgs);
            }
            else
            {
                // 那就是 if (CurrentCacheDeviceInputPointList.Count == 0)
            }
        }
    }
}
