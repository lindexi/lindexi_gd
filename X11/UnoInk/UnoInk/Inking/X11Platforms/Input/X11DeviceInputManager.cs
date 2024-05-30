using CPF.Linux;
using Microsoft.Maui.Graphics;
using static CPF.Linux.XLib;

namespace UnoInk.Inking.X11Platforms.Input;

unsafe class X11DeviceInputManager
{
    public X11DeviceInputManager(X11InfoManager infoManager)
    {
        _infoManager = infoManager;

        var devices = (XIDeviceInfo*) XLib.XIQueryDevice(Display,
            (int) XiPredefinedDeviceId.XIAllMasterDevices, out int num);

        Console.WriteLine($"DeviceNumber={num}");
        XIDeviceInfo? pointerDevice = default;
        for (var c = 0; c < num; c++)
        {
            StaticDebugLogger.WriteLine($"XIDeviceInfo [{c}] {devices[c].Deviceid} {devices[c].Use}");

            if (devices[c].Use == XiDeviceType.XIMasterPointer)
            {
                pointerDevice = devices[c];
                break;
            }
        }

        PointerDevice = pointerDevice;

        // ABS_MT_TOUCH_MAJOR ABS_MT_TOUCH_MINOR
        // https://www.kernel.org/doc/html/latest/input/multi-touch-protocol.html
        var touchMajorAtom = XLib.XInternAtom(Display, "Abs MT Touch Major", false);
        var touchMinorAtom = XLib.XInternAtom(Display, "Abs MT Touch Minor", false);
        IntPtr pressureAtom = XLib.XInternAtom(Display, "Abs MT Pressure", false);

        StaticDebugLogger.WriteLine(
            $"ABS_MT_TOUCH_MAJOR={touchMajorAtom} Name={XLib.GetAtomName(Display, touchMajorAtom)} ABS_MT_TOUCH_MINOR={touchMinorAtom} Name={XLib.GetAtomName(Display, touchMinorAtom)} Abs_MT_Pressure={pressureAtom} Name={XLib.GetAtomName(Display, pressureAtom)}");

        if (pointerDevice != null)
        {
            StaticDebugLogger.WriteLine($"pointerDevice.Value.NumClasses={pointerDevice.Value.NumClasses}");

            for (int i = 0; i < pointerDevice.Value.NumClasses; i++)
            {
                var xiAnyClassInfo = pointerDevice.Value.Classes[i];
                if (xiAnyClassInfo->Type == XiDeviceClass.XIValuatorClass)
                {
                    _valuators.Add(*((XIValuatorClassInfo**) pointerDevice.Value.Classes)[i]);
                }
                else if (xiAnyClassInfo->Type == XiDeviceClass.XIScrollClass)
                {
                    _scrollers.Add(*((XIScrollClassInfo**) pointerDevice.Value.Classes)[i]);
                }
            }

            foreach (var xiValuatorClassInfo in _valuators)
            {
                //var label = xiValuatorClassInfo.Label;
                //// 不能通过 Marshal.PtrToStringAnsi 读取 Label 的值 读取不到
                ////Marshal.PtrToStringAnsi(xiValuatorClassInfo.Label);
                //Console.WriteLine($"[Valuator] [{GetAtomName(Display, label)}] Label={label} Type={xiValuatorClassInfo.Type} Sourceid={xiValuatorClassInfo.Sourceid} Number={xiValuatorClassInfo.Number} Min={xiValuatorClassInfo.Min} Max={xiValuatorClassInfo.Max} Value={xiValuatorClassInfo.Value} Resolution={xiValuatorClassInfo.Resolution} Mode={xiValuatorClassInfo.Mode}");
                if (xiValuatorClassInfo.Label == touchMajorAtom)
                {
                    TouchMajorValuatorClassInfo = xiValuatorClassInfo;
                }
                else if (xiValuatorClassInfo.Label == touchMinorAtom)
                {
                    TouchMinorValuatorClassInfo = xiValuatorClassInfo;
                }
                else if (xiValuatorClassInfo.Label == pressureAtom)
                {
                    PressureValuatorClassInfo = xiValuatorClassInfo;
                }
            }
        }
    }

    private readonly X11InfoManager _infoManager;
    private IntPtr Display => _infoManager.Display;

    public XIDeviceInfo? PointerDevice { get; }

    public XIValuatorClassInfo? TouchMajorValuatorClassInfo { get; }

    public XIValuatorClassInfo? TouchMinorValuatorClassInfo { get; }

    public XIValuatorClassInfo? PressureValuatorClassInfo { get; }

    public IReadOnlyList<XIValuatorClassInfo> Valuators => _valuators;
    public IReadOnlyList<XIScrollClassInfo> Scrollers => _scrollers;

    private readonly List<XIValuatorClassInfo> _valuators = new List<XIValuatorClassInfo>();
    private readonly List<XIScrollClassInfo> _scrollers = new List<XIScrollClassInfo>();

    /// <summary>
    /// 调度消息处理
    /// </summary>
    /// <param name="xiDeviceEvent"></param>
    /// <returns>返回 false 代表消息不能被处理</returns>
    public void DispatchMessage(XIDeviceEvent* xiDeviceEvent)
    {
        var (shouldIgnore, deviceInputArgs) = ParseDeviceInputArgs(xiDeviceEvent);
        if (shouldIgnore)
        {
            // 比如这是模拟的鼠标消息
            return;
        }

        var xiEvent = xiDeviceEvent;
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
        for (var c = 0; c < xiDeviceEvent->valuators.MaskLen * 8; c++)
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
        var valuators = Valuators;

        foreach (var (key, value) in valuatorDictionary)
        {
            if (key == touchMajorValuatorClassInfo?.Number)
            {
                physicalWidth = value / touchMajorValuatorClassInfo.Value.Resolution *
                                _infoManager.ScreenPhysicalWidthCentimetre;

                pixelWidth = (value - touchMajorValuatorClassInfo.Value.Min) /
                             (touchMajorValuatorClassInfo.Value.Max -
                              touchMajorValuatorClassInfo.Value.Min) *
                             _infoManager.XDisplayWidth;
            }
            else if (key == touchMinorValuatorClassInfo?.Number)
            {
                physicalHeight = value / touchMinorValuatorClassInfo.Value.Resolution *
                                 _infoManager.ScreenPhysicalHeightCentimetre;

                pixelHeight = (value - touchMinorValuatorClassInfo.Value.Min) /
                              (touchMinorValuatorClassInfo.Value.Max -
                               touchMinorValuatorClassInfo.Value.Min) *
                              _infoManager.XDisplayHeight;
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

        var deviceInputPoint = new DeviceInputPoint(new Point(xiDeviceEvent->event_x, xiDeviceEvent->event_y), timestamp)
        {
            Pressure = pressure,
            PhysicalWidth = physicalWidth,
            PhysicalHeight = physicalHeight,

            PixelWidth = pixelWidth,
            PixelHeight = pixelHeight
        };

        //StaticDebugLogger.WriteLine($"[{xiDeviceEvent->evtype}][{id}] {deviceInputPoint.Position.X:0.00},{deviceInputPoint.Position.Y:0.00} WH:{physicalWidth:0.00},{physicalHeight:0.00} valuator:{valuatorDictionary.Count}");

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
