using System.Runtime.Versioning;
using CPF.Linux;
using static CPF.Linux.XLib;
using static CPF.Linux.ShapeConst;

namespace UnoInk.Inking.X11Platforms;

[SupportedOSPlatform("Linux")]
public class X11Window
{
    public X11Window(X11Application application, X11WindowCreateInfo createInfo) : this(application, CreateX11Window(application, createInfo))
    {
    }

    public X11Window(X11Application application, IntPtr x11WindowIntPtr)
    {
        Application = application;
        X11WindowIntPtr = x11WindowIntPtr;
    }

    public X11Application Application { get; }
    public IntPtr X11WindowIntPtr { get; }
    private X11InfoManager X11Info => Application.X11Info;

    public IntPtr GC => _gc ??= XCreateGC(X11Info.Display, X11WindowIntPtr, 0, 0);

    private IntPtr? _gc;

    public void ShowActive()
    {
        XLib.XMapWindow(X11Info.Display, X11WindowIntPtr);
    }

    /// <summary>
    /// 点击命中穿透
    /// </summary>
    public void SetClickThrough()
    {
        // 设置不接受输入
        // 这样输入穿透到后面一层里，由后面一层将内容上报上来
        var region = XCreateRegion();
        XShapeCombineRegion(X11Info.Display, X11WindowIntPtr, ShapeInput, 0, 0, region, ShapeSet);
    }

    public void SetOwner(IntPtr ownerX11WindowIntPtr)
    {
        XSetTransientForHint(X11Info.Display, X11WindowIntPtr, ownerX11WindowIntPtr);
    }

    private static IntPtr CreateX11Window(X11Application application, X11WindowCreateInfo createInfo)
    {
        var x11Info = application.X11Info;
        var display = x11Info.Display;
        var rootWindow = x11Info.RootWindow;
        var screen = x11Info.Screen;

        var width = createInfo.Width;
        var height = createInfo.Height;

        if (createInfo.IsFullScreen)
        {
            width = x11Info.XDisplayWidth;
            height = x11Info.XDisplayHeight;
        }

        XLib.XMatchVisualInfo(display, screen, 32, 4, out var info);
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
            colormap = XLib.XCreateColormap(display, rootWindow, visual, 0),
            border_pixel = 0,
            background_pixel = IntPtr.Zero,
        };

        var x11Window = XLib.XCreateWindow(display, rootWindow, 0, 0, width, height, 5,
            32,
            (int) CreateWindowArgs.InputOutput,
            visual,
            (nuint) valueMask, ref xSetWindowAttributes);

        XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                                 XEventMask.PointerMotionHintMask;
        var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
        XLib.XSelectInput(display, x11Window, mask);

        return x11Window;
    }
    
    public IDispatcher GetDispatcher()
        => new X11InkWindowDispatcher(this);

    public void ShowTaskbarIcon(bool value)
    {
        var _NET_WM_STATE_SKIP_TASKBAR = XInternAtom(X11Info.Display, "_NET_WM_STATE_SKIP_TASKBAR", false);
        ChangeWMAtoms(!value, _NET_WM_STATE_SKIP_TASKBAR);
    }

    private void ChangeWMAtoms(bool enable, params IntPtr[] atoms)
    {
        if (atoms.Length != 1 && atoms.Length != 2)
        {
            throw new ArgumentException();
        }

        //if (!_mapped)
        //{
        //    XGetWindowProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_STATE, IntPtr.Zero, new IntPtr(256),
        //        false, (IntPtr) Atom.XA_ATOM, out _, out _, out var nitems, out _,
        //        out var prop);
        //    var ptr = (IntPtr*) prop.ToPointer();
        //    var newAtoms = new HashSet<IntPtr>();
        //    for (var c = 0; c < nitems.ToInt64(); c++)
        //        newAtoms.Add(*ptr);
        //    XFree(prop);
        //    foreach (var atom in atoms)
        //        if (enable)
        //            newAtoms.Add(atom);
        //        else
        //            newAtoms.Remove(atom);

        //    XChangeProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_STATE, (IntPtr) Atom.XA_ATOM, 32,
        //        PropertyMode.Replace, newAtoms.ToArray(), newAtoms.Count);
        //}
        var wmState = XInternAtom(X11Info.Display, "_NET_WM_STATE", true);

        SendNetWMMessage(wmState,
            (IntPtr) (enable ? 1 : 0),
            atoms[0],
            atoms.Length > 1 ? atoms[1] : IntPtr.Zero,
            atoms.Length > 2 ? atoms[2] : IntPtr.Zero,
            atoms.Length > 3 ? atoms[3] : IntPtr.Zero
         );
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
                ptr4 = l3 ?? IntPtr.Zero
            }
        };
        xev.ClientMessageEvent.ptr4 = l4 ?? IntPtr.Zero;
        XSendEvent(X11Info.Display, X11Info.RootWindow, false,
            new IntPtr((int) (EventMask.SubstructureRedirectMask | EventMask.SubstructureNotifyMask)), ref xev);
    }

    public void EnterFullScreen(bool topmost)
    {
        // 下面是进入全屏
        var display = X11Info.Display;
        var hintsPropertyAtom = X11Info.HintsPropertyAtom;
        XChangeProperty(display, X11WindowIntPtr, hintsPropertyAtom, hintsPropertyAtom, 32, PropertyMode.Replace, new uint[5]
        {
            2, // flags : Specify that we're changing the window decorations.
            0, // functions
            0, // decorations : 0 (false) means that window decorations should go bye-bye.
            0, // inputMode
            0, // status
        }, 5);
        
        ChangeWMAtoms(false, XInternAtom(display, "_NET_WM_STATE_HIDDEN", true));
        ChangeWMAtoms(true, XInternAtom(display, "_NET_WM_STATE_FULLSCREEN", true));
        ChangeWMAtoms(false, XInternAtom(display, "_NET_WM_STATE_MAXIMIZED_VERT", true), XInternAtom(display, "_NET_WM_STATE_MAXIMIZED_HORZ", true));
        
        if (topmost)
        {
            // 在 UNO 下，将会导致停止渲染
            var topmostAtom = XInternAtom(display, "_NET_WM_STATE_ABOVE", true);
            SendNetWMMessage(X11Info.WMStateAtom, new IntPtr(1), topmostAtom);
        }
    }
}

[SupportedOSPlatform("Linux")]
file class X11InkWindowDispatcher : IDispatcher
{
    public X11InkWindowDispatcher(X11Window x11InkWindow)
    {
        _x11InkWindow = x11InkWindow;
    }
    
    private readonly X11Window _x11InkWindow;
    
    public bool TryEnqueue(Action action)
    {
        _ = _x11InkWindow.Application.X11PlatformThreading?.InvokeAsync(action, _x11InkWindow.X11WindowIntPtr);
        
        return true;
    }
    
    public async ValueTask<TResult> ExecuteAsync<TResult>(AsyncFunc<TResult> action, CancellationToken cancellation)
    {
        var taskCompletionSource = new TaskCompletionSource<TResult>();
        
        // 以下是兼容实现
        _ = _x11InkWindow.Application.X11PlatformThreading?.InvokeAsync(async () =>
        {
            try
            {
                var result = await action(cancellation);
                // 其实不支持同步上下文返回
                taskCompletionSource.SetResult(result);
            }
            catch (Exception e)
            {
                taskCompletionSource.SetException(e);
            }
        }, _x11InkWindow.X11WindowIntPtr);
        
        return await taskCompletionSource.Task;
    }
    
    public bool HasThreadAccess => _x11InkWindow.Application.X11PlatformThreading?.HasThreadAccess ?? false;
}
