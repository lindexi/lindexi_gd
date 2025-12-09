using DotNetCampus.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using X11ApplicationFramework.Natives;

using static X11ApplicationFramework.Natives.XLib;
using static X11ApplicationFramework.Natives.ShapeConst;

namespace X11ApplicationFramework.Apps;

/// <summary>
/// 封装一些 X11 窗口调用方法
/// </summary>
[SupportedOSPlatform("Linux")]
public class X11WindowNativeInterop
{
    public X11WindowNativeInterop(IntPtr display, IntPtr x11WindowIntPtr) : this(new X11InfoManager(display), x11WindowIntPtr)
    {
    }

    public X11WindowNativeInterop(X11InfoManager x11Info, IntPtr x11WindowIntPtr)
    {
        X11Info = x11Info;
        X11WindowIntPtr = x11WindowIntPtr;
    }

    public IntPtr Display => X11Info.Display;

    public IntPtr X11WindowIntPtr { get; }

    public X11InfoManager X11Info { get; }

    public bool IsShowing { get; private set; }

    public void ShowActive()
    {
        Log.Debug($"窗口显示");
        XMapWindow(Display, X11WindowIntPtr);
        XFlush(Display);
        IsShowing = true;
    }

    public void Hide()
    {
        XUnmapWindow(Display, X11WindowIntPtr);
        XFlush(Display);
        IsShowing = false;
    }

    public bool IsClosed { get; private set; }

    public void Close()
    {
        if (IsClosed)
        {
            throw new InvalidOperationException($"窗口已经关闭，不能重复关闭");
        }

        XDestroyWindow(Display, X11WindowIntPtr);
        XFlush(Display);
        OnClosed();
        IsClosed = true;
    }

    protected virtual void OnClosed()
    {
    }

    /// <summary>
    /// 点击命中穿透
    /// </summary>
    public void SetClickThrough()
    {
        // 设置不接受输入
        // 这样输入穿透到后面一层里，由后面一层将内容上报上来
        var region = XCreateRegion();
        XShapeCombineRegion(Display, X11WindowIntPtr, ShapeInput, 0, 0, region, ShapeSet);
        IsSetClickThrough = true;
    }

    public bool IsSetClickThrough { get; private set; }

    [Obsolete("请使用 SetOwner 代替，这个方法的作用只是让你知道有 SetOwner 方法可以调用而已")]
    public void SetParent(IntPtr parent) => SetOwner(parent);

    public void SetOwner(IntPtr ownerX11WindowIntPtr)
    {
        Log.Debug($"[X11WindowNativeInterop] SetOwner ownerX11WindowIntPtr={ownerX11WindowIntPtr}");
        XSetTransientForHint(Display, X11WindowIntPtr, ownerX11WindowIntPtr);
    }

    /// <summary>
    /// 通过 Unmap 和 map 的方法重新设置 Owner 关系。此时可能存在的问题是 App X KWin 三方无法正确同步关系，如丢失全屏等问题。这是因为在 Unmap 时，会清空窗口属性，在后续设置窗口属性时，不一定能处理好多进程重复调用
    /// </summary>
    /// <param name="ownerX11WindowIntPtr"></param>
    public void ResetOwnerByUnmapAndMap(IntPtr ownerX11WindowIntPtr)
    {
        Log.Info($"[X11WindowNativeInterop] ResetOwnerByUnmapAndMap OwnerX11WindowIntPtr={ownerX11WindowIntPtr}");

        XUnmapWindow(Display, X11WindowIntPtr);
        XFlush(Display);

        // 如果快速的 Unmap 和 Map，可能会导致 X11 窗口管理器没有正确的处理

        XMapWindow(Display, X11WindowIntPtr);
        XSetTransientForHint(Display, X11WindowIntPtr, ownerX11WindowIntPtr);
        XFlush(Display);
    }

    [Obsolete("此方式是不稳妥的，只靠等待，稳妥的是 unmap X11WindowIntPtr 再 map 再设置 Owner 关系")]
    public async Task ResetOwnerAsync(IntPtr ownerX11WindowIntPtr)
    {
        // 用于修复 ownerX11WindowIntPtr 被 unmap 过的情况
        // 详细测试代码：https://github.com/lindexi/lindexi_gd/tree/7b5489fed420c4239d8635b4c826ceb690fbd773/X11/JawalwhofuYageakaje
        var display = Display;
        IntPtr XA_WM_TRANSIENT_FOR = (IntPtr) 68;
        XDeleteProperty(display, X11WindowIntPtr, XA_WM_TRANSIENT_FOR);
        XFlush(display);

        // 去掉等待将无效。这是不稳妥的。在 demo 上测试没问题，但是在本项目测试是需要更大时间延迟，说不定在用户设备上再长都是无效的
        await Task.Delay(200);

        XSetTransientForHint(display, X11WindowIntPtr, ownerX11WindowIntPtr);
        XFlush(display);
    }

    public void RegisterMultiTouch([DisallowNull] XIDeviceInfo? pointerDevice)
    {
        XiEventType[] multiTouchEventTypes =
        [
            XiEventType.XI_TouchBegin,
            XiEventType.XI_TouchUpdate,
            XiEventType.XI_TouchEnd
        ];

        XiEventType[] defaultEventTypes =
        [
            XiEventType.XI_Motion,
            XiEventType.XI_ButtonPress,
            XiEventType.XI_ButtonRelease,
            XiEventType.XI_Leave,
            XiEventType.XI_Enter,
            XiEventType.XI_DeviceChanged,
        ];

        List<XiEventType> eventTypes = [.. multiTouchEventTypes, .. defaultEventTypes];

        XiSelectEvents(Display, X11WindowIntPtr, new Dictionary<int, List<XiEventType>> { [pointerDevice.Value.Deviceid] = eventTypes });
    }

    /// <summary>
    /// 是否显示任务栏图标
    /// </summary>
    /// <remarks>在麒麟系统上，需要在 MapWindow 之前调用，否则无效</remarks>
    /// <param name="value"></param>
    public void ShowTaskbarIcon(bool value)
    {
        var _NET_WM_STATE_SKIP_TASKBAR = XInternAtom(X11Info.Display, "_NET_WM_STATE_SKIP_TASKBAR", false);
        ChangeWMAtoms(!value, _NET_WM_STATE_SKIP_TASKBAR);
    }

    private unsafe void ChangeWMAtoms(bool enable, params IntPtr[] atoms)
    {
        if (atoms.Length != 1 && atoms.Length != 2)
        {
            throw new ArgumentException();
        }

        var wmState = XInternAtom(X11Info.Display, "_NET_WM_STATE", true);

        var isMapped = IsMapped;

        if (!isMapped)
        {
            XGetWindowProperty(X11Info.Display, X11WindowIntPtr, wmState, IntPtr.Zero, new IntPtr(256),
                false, (IntPtr) Atom.XA_ATOM, out _, out _, out var nitems, out _,
                out var prop);
            var ptr = (IntPtr*) prop.ToPointer();
            var newAtoms = new HashSet<IntPtr>();
            for (var c = 0; c < nitems.ToInt64(); c++)
            {
                newAtoms.Add(*ptr);
                // 修复只有第一个生效
                // https://github.com/AvaloniaUI/Avalonia/pull/16110
                ptr++;
            }

            XFree(prop);
            foreach (var atom in atoms)
            {
                if (enable)
                {
                    newAtoms.Add(atom);
                }
                else
                {
                    newAtoms.Remove(atom);
                }
            }

            XChangeProperty(X11Info.Display, X11WindowIntPtr, wmState, (IntPtr) Atom.XA_ATOM, 32,
                PropertyMode.Replace, newAtoms.ToArray(), newAtoms.Count);

            Log.Debug($"ChangeWMAtoms: {(enable ? "Enable" : "Disable")} atoms: {string.Join(", ", atoms.Select(a => a.ToString("X")))} ;  IsMapped={IsMapped}");
        }

        // 仿照 Avalonia 的写法，无论是否 Map 窗口都发送消息
        SendNetWMMessage(wmState,
            (IntPtr) (enable ? 1 : 0),
            atoms[0],
            atoms.Length > 1 ? atoms[1] : IntPtr.Zero,
            atoms.Length > 2 ? atoms[2] : IntPtr.Zero,
            atoms.Length > 3 ? atoms[3] : IntPtr.Zero
        );

        Log.Debug($"ChangeWMAtoms: {(enable ? "Enable" : "Disable")} atoms: {string.Join(", ", atoms.Select(a => a.ToString("X")))} ;  SendNetWMMessage");
    }

    public void SendNetWMMessage(IntPtr message_type, IntPtr l0,
        IntPtr? l1 = null, IntPtr? l2 = null, IntPtr? l3 = null, IntPtr? l4 = null)
    {
        var xev = new XEvent
        {
            ClientMessageEvent =
            {
                type = XEventName.ClientMessage,
                send_event = true,
                window = X11WindowIntPtr,
                message_type = message_type,
                format = 32,
                ptr1 = l0,
                ptr2 = l1 ?? IntPtr.Zero,
                ptr3 = l2 ?? IntPtr.Zero,
                ptr4 = l3 ?? IntPtr.Zero,
                ptr5 = l4 ?? IntPtr.Zero,
            }
        };
        XSendEvent(Display, X11Info.RootWindow, false,
            new IntPtr((int) (EventMask.SubstructureRedirectMask | EventMask.SubstructureNotifyMask)), ref xev);
    }

    public void EnterFullScreen(bool topmost)
    {
        // 下面是进入全屏
        var display = Display;
        var hintsPropertyAtom = X11Info.HintsPropertyAtom;
        XChangeProperty(display, X11WindowIntPtr, hintsPropertyAtom, hintsPropertyAtom, 32, PropertyMode.Replace, new nint[5]
        {
            2, // flags : Specify that we're changing the window decorations.
            0, // functions
            0, // decorations : 0 (false) means that window decorations should go bye-bye.
            0, // inputMode
            0, // status
        }, 5);

        ChangeWMAtoms(false, XInternAtom(display, "_NET_WM_STATE_HIDDEN", true));
        ChangeWMAtoms(true, XInternAtom(display, "_NET_WM_STATE_SKIP_TASKBAR", true));
        ChangeWMAtoms(true, XInternAtom(display, "_NET_WM_STATE_FULLSCREEN", true));
        ChangeWMAtoms(false, XInternAtom(display, "_NET_WM_STATE_MAXIMIZED_VERT", true), XInternAtom(display, "_NET_WM_STATE_MAXIMIZED_HORZ", true));

        if (topmost)
        {
            // 在 UNO 下，将会导致停止渲染
            var topmostAtom = XInternAtom(display, "_NET_WM_STATE_ABOVE", true);
            SendNetWMMessage(X11Info.WMStateAtom, new IntPtr(1), topmostAtom);
        }
    }

    /// <summary>
    /// 移动当前全屏窗口到指定的显示器
    /// </summary>
    /// <param name="monitorId"></param>
    public void MoveFullScreenWindowToMonitor(string monitorId)
    {
        var monitorInfos = Randr15ScreensImpl.GetMonitorInfos(Display, X11WindowIntPtr);

        for (var i = 0; i < monitorInfos.Length; i++)
        {
            if (monitorInfos[i].GetNameText() == monitorId)
            {
                Log.Info($"[X11WindowNativeInterop] MoveFullScreenWindowToMonitor Find Monitor={monitorId} Index={i}");
                MoveFullScreenWindowToMonitor(i);
                return;
            }
        }

        Log.Warn($"[X11WindowNativeInterop] MoveFullScreenWindowToMonitor can not find Monitor={monitorId}");
    }

    /// <summary>
    /// 移动当前全屏窗口到指定的显示器
    /// </summary>
    /// <param name="monitorIndex"></param>
    /// 参阅 [X11 设置多屏下窗口在哪个屏幕上全屏 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/19027800 )
    public void MoveFullScreenWindowToMonitor(int monitorIndex)
    {
        // [Window Manager Protocols | Extended Window Manager Hints](https://specifications.freedesktop.org/wm-spec/1.5/ar01s06.html )
        // 6.3 _NET_WM_FULLSCREEN_MONITORS
        // A read-only list of 4 monitor indices indicating the top, bottom, left, and right edges of the window when the fullscreen state is enabled. The indices are from the set returned by the Xinerama extension.
        // Windows transient for the window with _NET_WM_FULLSCREEN_MONITORS set, such as those with type _NEW_WM_WINDOW_TYPE_DIALOG, are generally expected to be positioned (e.g. centered) with respect to only one of the monitors. This might be the monitor containing the mouse pointer or the monitor containing the non-full-screen window.
        // A Client wishing to change this list MUST send a _NET_WM_FULLSCREEN_MONITORS client message to the root window. The Window Manager MUST keep this list updated to reflect the current state of the window.

        var wmState = XInternAtom(Display, "_NET_WM_FULLSCREEN_MONITORS", true);
        Log.Debug($"[X11WindowNativeInterop] MoveFullScreenWindowToMonitor _NET_WM_FULLSCREEN_MONITORS={wmState}");

        // 如 https://github.com/underdoeg/ofxFenster/blob/6ecd5bd9b8412f98e1c4e73433e2aade2b5902c4/src/ofxFenster.cpp#L691 的代码所示。这里传入的 Left、Top、Right、Bottom 不是像素的值，而是屏幕的索引值

        // _NET_WM_FULLSCREEN_MONITORS, CARDINAL[4]/32
        /*
         data.l[0] = the monitor whose top edge defines the top edge of the fullscreen window
         data.l[1] = the monitor whose bottom edge defines the bottom edge of the fullscreen window
         data.l[2] = the monitor whose left edge defines the left edge of the fullscreen window
         data.l[3] = the monitor whose right edge defines the right edge of the fullscreen window
        */
        // 这里的 Left、Top、Right、Bottom 是屏幕的索引值，而不是像素的值
        //var left = X;
        //var top = Y;
        //var right = X + Width;
        //var bottom = Y + Height;

        var left = monitorIndex;
        var top = monitorIndex;
        var right = monitorIndex;
        var bottom = monitorIndex;

        //Console.WriteLine($"[X11WindowNativeInterop] MoveFullScreenWindowToMonitor Left={left} Top={top} Right={right} Bottom={bottom}");

        //int[] monitorEdges = [top, bottom, left, right];
        //XChangeProperty(Display, X11Window, wmState, (IntPtr) Atom.XA_CARDINAL, format: 32, PropertyMode.Replace,
        //    monitorEdges, monitorEdges.Length);

        // A Client wishing to change this list MUST send a _NET_WM_FULLSCREEN_MONITORS client message to the root window. The Window Manager MUST keep this list updated to reflect the current state of the window.
        var xev = new XEvent
        {
            ClientMessageEvent =
            {
                type = XEventName.ClientMessage,
                send_event = true,
                window = X11WindowIntPtr,
                message_type = wmState,
                format = 32,
                ptr1 = top,
                ptr2 = bottom,
                ptr3 = left,
                ptr4 = right,
            }
        };

        XSendEvent(Display, X11Info.RootWindow, false,
            new IntPtr((int) (EventMask.SubstructureRedirectMask | EventMask.SubstructureNotifyMask)), ref xev);
    }

    public void AppendPid()
    {
        // The type of `_NET_WM_PID` is `CARDINAL` which is 32-bit unsigned integer, see https://specifications.freedesktop.org/wm-spec/1.3/ar01s05.html
<<<<<<< HEAD
        var _NET_WM_PID = XInternAtom(Display, "_NET_WM_PID", true);
        IntPtr XA_CARDINAL = (IntPtr) 6;
=======
        var _NET_WM_PID = XLib.XInternAtom(Display, "_NET_WM_PID", true);
<<<<<<< HEAD
        IntPtr XA_CARDINAL = X11Info.X11Atoms.XA_CARDINAL;
>>>>>>> f17892e1bcef253173ca643fcdedc91bec64aadf
=======
        IntPtr XA_CARDINAL = X11Atoms.XA_CARDINAL;
>>>>>>> c57652985be3eaa800d7536537d648ac2021917f
        var pid = (uint) Environment.ProcessId;
        XChangeProperty(Display, X11WindowIntPtr,
            _NET_WM_PID, XA_CARDINAL, 32,
            PropertyMode.Replace, ref pid, 1);
    }

    public void SetNetWmWindowTypeNormal()
    {
        var _NET_WM_WINDOW_TYPE_NORMAL = XInternAtom(Display, "_NET_WM_WINDOW_TYPE_NORMAL", true);
        var _NET_WM_WINDOW_TYPE = XInternAtom(Display, "_NET_WM_WINDOW_TYPE", true);

        XChangeProperty(Display, X11WindowIntPtr, _NET_WM_WINDOW_TYPE, (IntPtr) Atom.XA_ATOM,
            32, PropertyMode.Replace, new[] { _NET_WM_WINDOW_TYPE_NORMAL }, 1);
    }

    public bool IsMapped { get; private set; }

    protected unsafe bool OnReceiveEvent(XEvent* @event)
    {
        if (@event->type == XEventName.MapNotify)
        {
            IsMapped = true;
            var xMappingEvent = (XMappingEvent*) @event;
            OnMapped(xMappingEvent);
        }
        else if (@event->type == XEventName.UnmapNotify)
        {
            IsMapped = false;
            var xUnmapEvent = (XUnmapEvent*) @event;
            OnUnmapped(xUnmapEvent);
        }
        else
        {
            return false;
        }

        return true;
    }

    protected virtual unsafe void OnMapped(XMappingEvent* mappingEvent)
    {
    }

    protected virtual unsafe void OnUnmapped(XUnmapEvent* xUnmapEvent)
    {
    }
}