using System.Diagnostics;
using CPF.Linux;

using SkiaSharp;

using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using static CPF.Linux.XLib;

XInitThreads();
var display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(display);
var rootWindow = XDefaultRootWindow(display);

int major = 2, minor = 0;
XIQueryVersion(display, ref major, ref minor);

XMatchVisualInfo(display, screen, 32, 4, out var info);
var visual = info.visual;

var valueMask =
        //SetWindowValuemask.BackPixmap
        0
        | SetWindowValuemask.BackPixel
        | SetWindowValuemask.BorderPixel
        | SetWindowValuemask.BitGravity
        | SetWindowValuemask.WinGravity
        | SetWindowValuemask.BackingStore
        | SetWindowValuemask.ColorMap
    //| SetWindowValuemask.OverrideRedirect
    ;
var xSetWindowAttributes = new XSetWindowAttributes
{
    backing_store = 1,
    bit_gravity = Gravity.NorthWestGravity,
    win_gravity = Gravity.NorthWestGravity,
    //override_redirect = true, // 设置窗口的override_redirect属性为True，以避免窗口管理器的干预
    colormap = XCreateColormap(display, rootWindow, visual, 0),
    border_pixel = 0,
    background_pixel = 0,
};

var xDisplayWidth = XDisplayWidth(display, screen);
var xDisplayHeight = XDisplayHeight(display, screen);

var width = xDisplayWidth;
var height = xDisplayHeight;

var handle = XCreateWindow(display, rootWindow, 0, 0, width, height, 5,
    32,
    (int) CreateWindowArgs.InputOutput,
visual,
(nuint) valueMask, ref xSetWindowAttributes);
XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                         XEventMask.PointerMotionHintMask;
var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
XSelectInput(display, handle, mask);



var gc = XCreateGC(display, handle, 0, 0);
var skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
var skCanvas = new SKCanvas(skBitmap);
var xImage = CreateImage(skBitmap);

Console.WriteLine($"WH={width},{height}");

skCanvas.Clear(SKColors.Blue);

using var skPaint = new SKPaint();
skPaint.Color = SKColors.Black;
skPaint.StrokeWidth = 2;
skPaint.Style = SKPaintStyle.Stroke;
skPaint.IsAntialias = true;

//for (int y = 0; y < skBitmap.Height; y += 25)
//{
//    skPaint.Color = new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF);
//    skCanvas.DrawLine(0, y, skBitmap.Width, y, skPaint);
//}

//for (int x = 0; x < skBitmap.Width; x += 25)
//{
//    skPaint.Color = new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF);
//    skCanvas.DrawLine(x, 0, x, skBitmap.Height, skPaint);
//}

//skCanvas.Flush();

//skCanvas.Clear(SKColors.White);

// 随意用一个支持中文的字体
var typeface = SKFontManager.Default.MatchCharacter('十');
skPaint.TextSize = 20;
skPaint.Typeface = typeface;
skPaint.Color = SKColors.Black;
//skCanvas.DrawText("中文", 100, 100, skPaint); // 测试绘制中文
skCanvas.Clear(SKColors.White);

var touchMajorAtom = XInternAtom(display, "Abs MT Touch Major", false);
var touchMinorAtom = XInternAtom(display, "Abs MT Touch Minor", false);
var pressureAtom = XInternAtom(display, "Abs MT Pressure", false);

Console.WriteLine($"ABS_MT_TOUCH_MAJOR={touchMajorAtom} Name={XLib.GetAtomName(display, touchMajorAtom)} ABS_MT_TOUCH_MINOR={touchMinorAtom} Name={XLib.GetAtomName(display, touchMinorAtom)} Abs_MT_Pressure={pressureAtom} Name={XLib.GetAtomName(display, pressureAtom)}");

var valuators = new List<XIValuatorClassInfo>();
var scrollers = new List<XIScrollClassInfo>();

XIValuatorClassInfo? touchMajorValuatorClassInfo = null;
XIValuatorClassInfo? touchMinorValuatorClassInfo = null;
XIValuatorClassInfo? pressureValuatorClassInfo = null;

//Task.Run(() =>
//{
//    Console.ReadLine();

//    // 重新获取触摸宽度高度是可以获取到的
//    Console.WriteLine("重新获取");

//    unsafe
//    {
//        var stopwatch = Stopwatch.StartNew();
//        var devices = (XIDeviceInfo*) XIQueryDevice(display,
//            (int) XiPredefinedDeviceId.XIAllMasterDevices, out int num);
//        stopwatch.Stop();
//        // 统计到的耗时是 XIQueryDevice 耗时=7297ms
//        // 似乎没有点击屏幕时，是不会完成 XIQueryDevice 方法的
//        Console.WriteLine($"XIQueryDevice 耗时={stopwatch.ElapsedMilliseconds}ms");

//        for (var c = 0; c < num; c++)
//        {
//            if (devices[c].Use == XiDeviceType.XIMasterPointer)
//            {
//                var pointerDevice = devices[c];
//                Console.WriteLine($"XIMasterPointer Deviceid={pointerDevice.Deviceid}");

//                for (int i = 0; i < pointerDevice.NumClasses; i++)
//                {
//                    var xiAnyClassInfo = pointerDevice.Classes[i];
//                    if (xiAnyClassInfo->Type == XiDeviceClass.XIValuatorClass)
//                    {
//                        var xiValuatorClassInfo = *((XIValuatorClassInfo*) xiAnyClassInfo);

//                        Console.WriteLine($"XiValuatorClassInfo Label={xiValuatorClassInfo.Label}({XLib.GetAtomName(display, xiValuatorClassInfo.Label)}) Value={xiValuatorClassInfo.Value}; Max={xiValuatorClassInfo.Max:0.00}; Min={xiValuatorClassInfo.Min:0.00}; Resolution={xiValuatorClassInfo.Resolution})");
//                    }
//                }

//                var multiTouchEventTypes = new List<XiEventType>
//                {
//                    XiEventType.XI_TouchBegin,
//                    XiEventType.XI_TouchUpdate,
//                    XiEventType.XI_TouchEnd,

//                    XiEventType.XI_Motion,
//                    XiEventType.XI_ButtonPress,
//                    XiEventType.XI_ButtonRelease,
//                    XiEventType.XI_Leave,
//                    XiEventType.XI_Enter,
//                };

//                XiSelectEvents(display, handle, new Dictionary<int, List<XiEventType>>
//                {
//                    [pointerDevice.Deviceid] = multiTouchEventTypes,
//                });
//                break;
//            }
//        }

//    }
//});

unsafe
{
    var devices = (XIDeviceInfo*) XIQueryDevice(display,
        (int) XiPredefinedDeviceId.XIAllMasterDevices, out int num);

    XIDeviceInfo? pointerDevice = default;
    for (var c = 0; c < num; c++)
    {
        Console.WriteLine($"XIDeviceInfo [{c}] DeviceId={devices[c].Deviceid} Name={devices[c].Name:X}({Marshal.PtrToStringAnsi(devices[c].Name)}) Use={devices[c].Use} Attachment={devices[c].Attachment}");

        if (devices[c].Use == XiDeviceType.XIMasterPointer)
        {
            pointerDevice ??= devices[c];
            // 特意不用 break; 多次进入循环，用于输出更多调试信息
            continue;
        }
    }

    if (pointerDevice != null)
    {
        var multiTouchEventTypes = new List<XiEventType>
        {
            XiEventType.XI_TouchBegin,
            XiEventType.XI_TouchUpdate,
            XiEventType.XI_TouchEnd,

            XiEventType.XI_Motion,
            XiEventType.XI_ButtonPress,
            XiEventType.XI_ButtonRelease,
            XiEventType.XI_Leave,
            XiEventType.XI_Enter,

            XiEventType.XI_DeviceChanged,

            // 不能这么写，将会出现以下错误
            // X Error of failed request:  BadValue (integer parameter out of range for operation)
            // Major opcode of failed request:  131 (XInputExtension)
            // Minor opcode of failed request:  46 ()
            // Value in failed request:  0xb
            // Serial number of failed request:  22
            // Current serial number in output stream:  23
            //XiEventType.XI_HierarchyChanged,
        };

        XiSelectEvents(display, handle, new Dictionary<int, List<XiEventType>>
        {
            [pointerDevice.Value.Deviceid] = multiTouchEventTypes,
        });

        // 重新注册依然没有触摸宽度高度
        //Task.Run(() =>
        //{
        //    Console.ReadLine();
        //    Console.WriteLine("重新注册");

        //    XiSelectEvents(display, handle, new Dictionary<int, List<XiEventType>>
        //    {
        //        [pointerDevice.Value.Deviceid] = multiTouchEventTypes,
        //    });
        //});

        // 注册炸掉原因是使用了 XIAllMasterDevices 而不是 XIAllDevices 会炸掉
        // 以下注册炸掉
        // X Error of failed request:  BadValue (integer parameter out of range for operation)
        // Major opcode of failed request:  131 (XInputExtension)
        // Minor opcode of failed request:  46 ()
        // Value in failed request:  0xb
        // Serial number of failed request:  23
        // Current serial number in output stream:  24
        //XiSelectEvents(display, rootWindow, new Dictionary<int, List<XiEventType>>()
        //{
        //    [(int) XiPredefinedDeviceId.XIAllMasterDevices] = new List<XiEventType>()
        //    {
        //        XiEventType.XI_HierarchyChanged,
        //        XiEventType.XI_DeviceChanged,
        //    }
        //});

        // GTK3 gdkdevicemanager-xi2.c
        // XIEventMask event_mask;
        // unsigned char mask[2] = { 0 };
        // XISetMask (mask, XI_HierarchyChanged);
        // XISetMask (mask, XI_DeviceChanged);
        // XISetMask (mask, XI_PropertyEvent);
        // event_mask.deviceid = XIAllDevices;
        // event_mask.mask_len = sizeof (mask);
        // event_mask.mask = mask;
        // _gdk_x11_device_manager_xi2_select_events
        XiSelectEvents(display, rootWindow, new Dictionary<int, List<XiEventType>>()
        {
            [(int) XiPredefinedDeviceId.XIAllDevices] = new List<XiEventType>()
            {
                XiEventType.XI_HierarchyChanged,
            }
        });

        for (int i = 0; i < pointerDevice.Value.NumClasses; i++)
        {
            var xiAnyClassInfo = pointerDevice.Value.Classes[i];
            if (xiAnyClassInfo->Type == XiDeviceClass.XIValuatorClass)
            {
                valuators.Add(*((XIValuatorClassInfo**) pointerDevice.Value.Classes)[i]);
            }
            else if (xiAnyClassInfo->Type == XiDeviceClass.XIScrollClass)
            {
                scrollers.Add(*((XIScrollClassInfo**) pointerDevice.Value.Classes)[i]);
            }
            else if (xiAnyClassInfo->Type == XiDeviceClass.XITouchClass)
            {
                Console.WriteLine($"Touch Sourceid={xiAnyClassInfo->Sourceid}");
            }
        }

        foreach (var xiValuatorClassInfo in valuators)
        {
            if (xiValuatorClassInfo.Label == touchMajorAtom)
            {
                Console.WriteLine($"TouchMajorAtom Value={xiValuatorClassInfo.Value}; Max={xiValuatorClassInfo.Max:0.00}; Min={xiValuatorClassInfo.Min:0.00}; Resolution={xiValuatorClassInfo.Resolution}");

                touchMajorValuatorClassInfo = xiValuatorClassInfo;
            }
            else if (xiValuatorClassInfo.Label == touchMinorAtom)
            {
                Console.WriteLine($"TouchMinorAtom Value={xiValuatorClassInfo.Value}; Max={xiValuatorClassInfo.Max:0.00}; Min={xiValuatorClassInfo.Min:0.00}; Resolution={xiValuatorClassInfo.Resolution}");

                touchMinorValuatorClassInfo = xiValuatorClassInfo;
            }
            else if (xiValuatorClassInfo.Label == pressureAtom)
            {
                Console.WriteLine($"PressureAtom Value={xiValuatorClassInfo.Value}; Max={xiValuatorClassInfo.Max:0.00}; Min={xiValuatorClassInfo.Min:0.00}; Resolution={xiValuatorClassInfo.Resolution}");

                pressureValuatorClassInfo = xiValuatorClassInfo;
            }
            else
            {
                Console.WriteLine($"XiValuatorClassInfo Label={xiValuatorClassInfo.Label}({XLib.GetAtomName(display, xiValuatorClassInfo.Label)}) Value={xiValuatorClassInfo.Value}; Max={xiValuatorClassInfo.Max:0.00}; Min={xiValuatorClassInfo.Min:0.00}; Resolution={xiValuatorClassInfo.Resolution})");
            }
        }

        if (touchMajorValuatorClassInfo is null)
        {
            Console.WriteLine("Can't find TouchMajorAtom 丢失触摸宽度高度");
        }
    }
    else
    {
        Console.WriteLine("pointerDevice==null");
    }

    XIFreeDeviceInfo(devices);
}

// 先获取触摸再显示窗口，依然拿不到触摸宽度高度
XMapWindow(display, handle);
XFlush(display);

var dictionary = new Dictionary<int, TouchInfo>();
bool isSendExposeEvent = false;

var valuatorDictionary = new Dictionary<int, double>();

Action? action = null;

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);

    if (xNextEvent != 0)
    {
        break;
    }

    if (@event.type == XEventName.Expose)
    {
        XPutImage(display, handle, gc, ref xImage, @event.ExposeEvent.x, @event.ExposeEvent.y, @event.ExposeEvent.x, @event.ExposeEvent.y, (uint) @event.ExposeEvent.width,
            (uint) @event.ExposeEvent.height);
        isSendExposeEvent = false;
    }
    else if (@event.type == XEventName.MotionNotify)
    {
        var x = @event.MotionEvent.x;
        var y = @event.MotionEvent.y;

        action?.Invoke();
        action = null;

        skCanvas.Clear(new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF));
        skCanvas.Flush();

        SendExposeEvent(display, handle, 0, 0, width, height);
    }
    else if (@event.type == XEventName.GenericEvent)
    {
        unsafe
        {
            void* data = &@event.GenericEventCookie;
            XGetEventData(display, data);

            try
            {
                var xiEvent = (XIEvent*) @event.GenericEventCookie.data;
                if (xiEvent->evtype is
                    XiEventType.XI_ButtonRelease
                    or XiEventType.XI_ButtonRelease
                    or XiEventType.XI_Motion
                    or XiEventType.XI_TouchBegin
                    or XiEventType.XI_TouchUpdate
                    or XiEventType.XI_TouchEnd)
                {
                    var xiDeviceEvent = (XIDeviceEvent*) xiEvent;

                    var x = xiDeviceEvent->event_x;
                    var y = xiDeviceEvent->event_y;
                    if (xiEvent->evtype == XiEventType.XI_TouchBegin)
                    {
                        Console.WriteLine("XI_TouchBegin");
                        dictionary[xiDeviceEvent->detail] = new TouchInfo(xiDeviceEvent->detail, x, y, -1, -1, false);

                        valuatorDictionary.Clear();
                        var values = xiDeviceEvent->valuators.Values;
                        for (var c = 0; c < xiDeviceEvent->valuators.MaskLen * 8/*一个 Byte 有 8 个 bit，以下 XIMaskIsSet 是按照 bit 进行判断的*/; c++)
                        {
                            if (XIMaskIsSet(xiDeviceEvent->valuators.Mask, c))
                            {
                                // 只有 Mask 存在值的，才能获取 Values 的值
                                valuatorDictionary[c] = *values;
                                values++;
                            }
                        }

                        bool readTouchMajor = false;
                        if (touchMinorValuatorClassInfo.HasValue)
                        {
                            if (valuatorDictionary.TryGetValue(touchMinorValuatorClassInfo.Value.Number, out var value))
                            {
                                readTouchMajor = true;
                            }
                        }

                        if (readTouchMajor)
                        {
                            Console.WriteLine($"读取到触摸宽度");
                        }
                        else
                        {
                            Console.WriteLine($"没有读取到触摸宽度");

                            foreach (var keyValuePair in valuatorDictionary)
                            {
                                Console.WriteLine($"Number={keyValuePair.Key} Value={keyValuePair.Value}");
                            }
                        }
                    }
                    else if (xiEvent->evtype == XiEventType.XI_TouchUpdate)
                    {
                        if (dictionary.TryGetValue(xiDeviceEvent->detail, out var t))
                        {
                            t = t with
                            {
                                X = x,
                                Y = y,
                            };

                            valuatorDictionary.Clear();
                            var values = xiDeviceEvent->valuators.Values;
                            for (var c = 0; c < xiDeviceEvent->valuators.MaskLen * 8/*一个 Byte 有 8 个 bit，以下 XIMaskIsSet 是按照 bit 进行判断的*/; c++)
                            {
                                if (XIMaskIsSet(xiDeviceEvent->valuators.Mask, c))
                                {
                                    // 只有 Mask 存在值的，才能获取 Values 的值
                                    valuatorDictionary[c] = *values;
                                    values++;
                                }
                            }

                            if (touchMajorValuatorClassInfo.HasValue)
                            {
                                if (valuatorDictionary.TryGetValue(touchMajorValuatorClassInfo.Value.Number, out var value))
                                {
                                    t = t with
                                    {
                                        TouchMajor = value,
                                    };
                                }
                                else
                                {

                                }
                            }

                            if (touchMinorValuatorClassInfo.HasValue)
                            {
                                if (valuatorDictionary.TryGetValue(touchMinorValuatorClassInfo.Value.Number, out var value))
                                {
                                    t = t with
                                    {
                                        TouchMinor = value,
                                    };
                                }
                                else
                                {

                                }
                            }

                            //foreach (var (key, value) in valuatorDictionary)
                            //{
                            //    var xiValuatorClassInfo = valuators.FirstOrDefault(t => t.Number == key);

                            //    //var label = GetAtomName(display, xiValuatorClassInfo.Label);

                            //    if (xiValuatorClassInfo.Label == touchMajorAtom)
                            //    {
                            //        //label = "TouchMajor";
                            //        t = t with
                            //        {
                            //            TouchMajor = value,
                            //        };
                            //    }
                            //    else if (xiValuatorClassInfo.Label == touchMinorAtom)
                            //    {
                            //        //label = "TouchMinor";
                            //        t = t with
                            //        {
                            //            TouchMinor = value,
                            //        };
                            //    }
                            //    else if (xiValuatorClassInfo.Label == pressureAtom)
                            //    {
                            //        //label = "Pressure";
                            //    }

                            //    //Console.WriteLine($"[Valuator] [{label}] Label={xiValuatorClassInfo.Label} Type={xiValuatorClassInfo.Type} Sourceid={xiValuatorClassInfo.Sourceid} Number={xiValuatorClassInfo.Number} Min={xiValuatorClassInfo.Min} Max={xiValuatorClassInfo.Max} Value={xiValuatorClassInfo.Value} Resolution={xiValuatorClassInfo.Resolution} Mode={xiValuatorClassInfo.Mode} Value={value}");
                            //}

                            dictionary[xiDeviceEvent->detail] = t;
                        }
                    }
                    else if (xiEvent->evtype == XiEventType.XI_TouchEnd)
                    {
                        if (dictionary.TryGetValue(xiDeviceEvent->detail, out var t))
                        {
                            dictionary[xiDeviceEvent->detail] = t with
                            {
                                X = x,
                                Y = y,
                                IsUp = true,
                            };
                        }
                    }

                    Draw();
                }
                else if (xiEvent->evtype is XiEventType.XI_Enter or XiEventType.XI_Leave or XiEventType.XI_Motion or XiEventType.XI_ButtonPress)
                {

                }
                else if (xiEvent->evtype is XiEventType.XI_DeviceChanged)
                {
                    Console.WriteLine($"设备变更");

                    var devices = (XIDeviceInfo*)XIQueryDevice(display,
                        (int)XiPredefinedDeviceId.XIAllMasterDevices, out int num);

                    XIDeviceInfo? pointerDevice = default;

                    for (var c = 0; c < num; c++)
                    {
                        if (devices[c].Use == XiDeviceType.XIMasterPointer)
                        {
                            pointerDevice = devices[c];
                            Console.WriteLine($"XIMasterPointer Deviceid={pointerDevice.Value.Deviceid}");

                            for (int i = 0; i < pointerDevice.Value.NumClasses; i++)
                            {
                                var xiAnyClassInfo = pointerDevice.Value.Classes[i];
                                if (xiAnyClassInfo->Type == XiDeviceClass.XIValuatorClass)
                                {
                                    var xiValuatorClassInfo = *((XIValuatorClassInfo*)xiAnyClassInfo);

                                    Console.WriteLine($"XiValuatorClassInfo Label={xiValuatorClassInfo.Label}({XLib.GetAtomName(display, xiValuatorClassInfo.Label)}) Num={xiValuatorClassInfo.Number} Value={xiValuatorClassInfo.Value}; Max={xiValuatorClassInfo.Max:0.00}; Min={xiValuatorClassInfo.Min:0.00}; Resolution={xiValuatorClassInfo.Resolution})");

                                    if (xiValuatorClassInfo.Label == touchMajorAtom)
                                    {
                                        touchMajorValuatorClassInfo = xiValuatorClassInfo;
                                    }
                                }
                            }
                        }
                    }

                    var multiTouchEventTypes = new List<XiEventType>
                    {
                        XiEventType.XI_TouchBegin,
                        XiEventType.XI_TouchUpdate,
                        XiEventType.XI_TouchEnd,

                        XiEventType.XI_Motion,
                        XiEventType.XI_ButtonPress,
                        XiEventType.XI_ButtonRelease,
                        XiEventType.XI_Leave,
                        XiEventType.XI_Enter,

                        XiEventType.XI_DeviceChanged,

                        // 不能这么写，将会出现以下错误
                        // X Error of failed request:  BadValue (integer parameter out of range for operation)
                        // Major opcode of failed request:  131 (XInputExtension)
                        // Minor opcode of failed request:  46 ()
                        // Value in failed request:  0xb
                        // Serial number of failed request:  22
                        // Current serial number in output stream:  23
                        //XiEventType.XI_HierarchyChanged,
                    };

                    // 尝试重新注册也是无效的，无法获取到触摸宽度高度
                    XiSelectEvents(display, handle, new Dictionary<int, List<XiEventType>>
                    {
                        [pointerDevice.Value.Deviceid] = [],
                    });


                    XFlush(display);

                    Console.WriteLine($"尝试禁用触摸");

                    action = () =>
                    {
                        Console.WriteLine($"重新注册触摸");
                        XiSelectEvents(display, handle, new Dictionary<int, List<XiEventType>>
                        {
                            [pointerDevice.Value.Deviceid] = multiTouchEventTypes,
                        });
                    };

                    XIFreeDeviceInfo(devices);
                }
                else
                {
                    Console.WriteLine($"xiEvent->evtype={xiEvent->evtype}");
                }
            }
            finally
            {

            }
        }
    }
    else if (@event.type is XEventName.PropertyNotify or XEventName.EnterNotify or XEventName.KeymapNotify or XEventName.LeaveNotify)
    {

    }
    else
    {
        Console.WriteLine($"Event={@event.type}");
    }
}

void Draw()
{
    skCanvas.Clear(SKColors.White);

    foreach (var value in dictionary.Values)
    {
        if (touchMajorValuatorClassInfo != null)
        {
            double pixelWidth = value.TouchMajor / touchMajorValuatorClassInfo.Value.Max * xDisplayWidth;
            double pixelHeight;
            if (touchMinorValuatorClassInfo is null)
            {
                pixelHeight = pixelWidth;
            }
            else
            {
                pixelHeight = value.TouchMinor / touchMinorValuatorClassInfo.Value.Max * xDisplayHeight;
            }

            skCanvas.DrawRect((float) (value.X - pixelWidth / 2), (float) (value.Y - pixelHeight / 2), (float) pixelWidth, (float) pixelHeight, skPaint);
        }

        skPaint.IsLinearText = false;
        var text = $"""Id={value.Id};X={value.X} Y={value.Y};W={value.TouchMajor} H={value.TouchMinor}""";
        if (value.IsUp)
        {
            text = "[已抬起];" + text;
        }

        skCanvas.DrawText(text, (float) value.X, (float) value.Y, skPaint);
    }

    if (isSendExposeEvent)
    {
        return;
    }

    SendExposeEvent(display, handle, 0, 0, width, height);
    isSendExposeEvent = true;
}


static XImage CreateImage(SKBitmap skBitmap)
{
    const int bytePerPixelCount = 4; // RGBA 一共4个 byte 长度
    var bitPerByte = 8;

    var bitmapWidth = skBitmap.Width;
    var bitmapHeight = skBitmap.Height;

    var img = new XImage();
    int bitsPerPixel = bytePerPixelCount * bitPerByte;
    img.width = bitmapWidth;
    img.height = bitmapHeight;
    img.format = 2; //ZPixmap;
    img.data = skBitmap.GetPixels();
    img.byte_order = 0; // LSBFirst;
    img.bitmap_unit = bitsPerPixel;
    img.bitmap_bit_order = 0; // LSBFirst;
    img.bitmap_pad = bitsPerPixel;
    img.depth = bitsPerPixel;
    img.bytes_per_line = bitmapWidth * bytePerPixelCount;
    img.bits_per_pixel = bitsPerPixel;
    XInitImage(ref img);

    return img;
}

static void SendExposeEvent(IntPtr display, IntPtr window, int x, int y, int width, int height)
{
    var exposeEvent = new XExposeEvent
    {
        type = XEventName.Expose,
        display = display,
        window = window,
        x = x,
        y = y,
        width = width,
        height = height,
        count = 1,
    };

    var xEvent = new XEvent
    {
        ExposeEvent = exposeEvent
    };

    XSendEvent(display, window, false, new IntPtr((int) (EventMask.ExposureMask)), ref xEvent);
    XFlush(display);
}

record TouchInfo(int Id, double X, double Y, double TouchMajor, double TouchMinor, bool IsUp);