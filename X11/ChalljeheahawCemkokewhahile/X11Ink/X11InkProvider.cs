using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Runtime.Versioning;
using UnoInk.Inking.X11Platforms;
using CPF.Linux;
using static CPF.Linux.XLib;

namespace ChalljeheahawCemkokewhahile.X11Ink;

[SupportedOSPlatform("Linux")]
internal class X11InkProvider : X11Application
{
    public X11InkProvider()
    {
        // 不需要调用了，因为在 UNO 底层已经调用
        //// 这句话不能调用多次 虽然调用多次不会炸
        // https://tronche.com/gui/x/xlib/display/XInitThreads.html
        // It is only necessary to call this function if multiple threads might use Xlib concurrently. If all calls to Xlib functions are protected by some other access mechanism (for example, a mutual exclusion lock in a toolkit or through explicit client programming), Xlib thread initialization is not required. It is recommended that single-threaded programs not call this function.
        //XInitThreads();
        //XInitThreads();
        //XInitThreads();
    }

    //[MemberNotNull(nameof(_x11InkWindow))]
    //public void Start(Window unoWindow)
    //{
    //    var x11WindowIntPtr =
    //        WindowInteropHelper.GetX11NativeWindow(unoWindow);

    //    var x11InkWindow = new X11InkWindow(this, x11WindowIntPtr);
    //    _x11InkWindow = x11InkWindow;

    //    // 创建窗口先，再启动消息循环。确保开启消息循环的时候必定有窗口，防止收到窗口消息的时候窗口还没创建

    //    if (!X11PlatformThreading.IsRunning)
    //    {
    //        // 卡住原因有可能是先跑起来消息循环导致的
    //        // 比如说可能的原因是多线程访问 Display 导致的问题
    //        base.Start();
    //    }
    //    else
    //    {
    //        Debug.Fail("大胆，谁让你多次启动的");
    //    }
    //}

    public event EventHandler<X11InkWindow>? X11InkWindowCreated;

    protected override void OnStart()
    {
        var x11InkWindow = new X11InkWindow(this, IntPtr.Zero, enableInput: true);
        _x11InkWindow = x11InkWindow;

        X11InkWindowCreated?.Invoke(this, x11InkWindow);

        X11PlatformThreading.Run();
    }

    /// <summary>
    /// 启动且设置传入的 <paramref name="x11WindowIntPtr"/> 作为 Owner 窗口
    /// </summary>
    /// <param name="x11WindowIntPtr"></param>
    [MemberNotNull(nameof(_x11InkWindow))]
    public void Start(IntPtr x11WindowIntPtr)
    {
        var x11InkWindow = new X11InkWindow(this, x11WindowIntPtr);
        _x11InkWindow = x11InkWindow;

        // 创建窗口先，再启动消息循环。确保开启消息循环的时候必定有窗口，防止收到窗口消息的时候窗口还没创建

        if (!X11PlatformThreading.IsRunning)
        {
            // 卡住原因有可能是先跑起来消息循环导致的
            // 比如说可能的原因是多线程访问 Display 导致的问题
            base.Start();
        }
        else
        {
            Debug.Fail("大胆，谁让你多次启动的");
        }
    }

    public X11InkWindow InkWindow
    {
        get
        {
            EnsureX11Start();
            return _x11InkWindow;
        }
    }

    private X11InkWindow? _x11InkWindow;

    [MemberNotNull(nameof(_x11InkWindow))]
    private void EnsureX11Start()
    {
        if (_x11InkWindow is null)
        {
            throw new InvalidOperationException();
        }
    }

    internal override unsafe void DispatchEvent(XEvent @event)
    {
        //Console.WriteLine($"[DispatchEvent] {@event.type}");
        if (_x11InkWindow is null)
        {
            // 这里可能是创建窗口内部进来的，比如全屏
            return;
        }

        if (@event.type == XEventName.Expose)
        {
            if (@event.ExposeEvent.window == InkWindow.X11InkWindowIntPtr)
            {
                InkWindow.Expose(@event.ExposeEvent);
                return;
            }
        }
        else if (@event.type == XEventName.GenericEvent)
        {
            void* data = &@event.GenericEventCookie;
            /*
             bing:
            `XGetEventData` 是一个用于 **X Window System** 的函数，其主要目的是通过 **cookie** 来检索和释放附加的事件数据。让我们来详细了解一下：
            
               - **函数名称**：`XGetEventData`
               - **功能**：检索通过 **cookie** 存储的附加事件数据。
               - **参数**：
                   - `display`：指定与 X 服务器的连接。
                   - `cookie`：指定要释放或检索数据的 **cookie**。
               - **结构体**：`XGenericEventCookie`
                   - `type`：事件类型。
                   - `serial`：事件序列号。
                   - `send_event`：是否为发送事件。
                   - `display`：指向 X 服务器的指针。
                   - `extension`：扩展信息。
                   - `evtype`：事件类型。
                   - `cookie`：唯一标识此事件的 **cookie**。
                   - `data`：事件数据的指针，在调用 `XGetEventData` 之前未定义。
               - **描述**：某些扩展的 `XGenericEvents` 需要额外的内存来存储信息。对于这些事件，库会返回一个具有唯一标识此事件的 **cookie** 的 `XGenericEventCookie`。直到调用 `XGetEventData`，`XGenericEventCookie` 的数据指针是未定义的。`XGetEventData` 函数检索给定 **cookie** 的附加数据。不需要与服务器进行往返通信。如果 **cookie** 无效或事件不是由 **cookie** 处理程序处理的事件，则返回 `False`。如果 `XGetEventData` 返回 `True`，则 **cookie** 的数据指针指向包含事件信息的内存。客户端必须调用 `XFreeEventData` 来释放此内存。对于同一事件 **cookie** 的多次调用，`XGetEventData` 返回 `False`。`XFreeEventData` 函数释放与 **cookie** 关联的数据。客户端必须对使用 `XGetEventData` 获得的每个 **cookie** 调用 `XFreeEventData`。
               - **注意事项**：
                   - 如果 **cookie** 已通过 `XNextEvent` 返回给客户端，但其数据尚未通过 `XGetEventData` 检索，则该 **cookie** 被定义为未声明。后续对 `XNextEvent` 的调用可能会释放与未声明 **cookie** 关联的内存。
                   - 多线程的 X 客户端必须确保在下一次调用 `XNextEvent` 之前调用 `XGetEventData`。
            
               更多信息，请参阅 [XGetEventData 文档](https://www.x.org/releases/X11R7.6/doc/man/man3/XGetEventData.3.xhtml)。¹²
            
               源: 与必应的对话， 2024/4/7
               (1) XGetEventData - X Window System. https://www.x.org/releases/X11R7.6/doc/man/man3/XGetEventData.3.xhtml.
               (2) XGetEventData(3) — libX11-devel. https://man.docs.euro-linux.com/EL%209/libX11-devel/XGetEventData.3.en.html.
               (3) X11R7.7 Manual Pages: Section 3: Library Functions - X Window System. https://www.x.org/releases/X11R7.7/doc/man/man3/.
             */
            XGetEventData(X11Info.Display, data);

            try
            {
                var xiEvent = (XIEvent*) @event.GenericEventCookie.data;
                if (xiEvent->evtype == XiEventType.XI_DeviceChanged)
                {
                }
                else if (xiEvent->evtype is
                         XiEventType.XI_ButtonPress
                         or XiEventType.XI_ButtonRelease
                         or XiEventType.XI_Motion
                         or XiEventType.XI_TouchBegin
                         or XiEventType.XI_TouchUpdate
                         or XiEventType.XI_TouchEnd)
                {
                    var xiDeviceEvent = (XIDeviceEvent*) xiEvent;

                    if (xiDeviceEvent->EventWindow == InkWindow.X11InkWindowIntPtr)
                    {
                        //Console.WriteLine($"调度消息");
                        InkWindow.X11DeviceInputManager.DispatchMessage(xiDeviceEvent);

                        // 这里调用 X11DeviceInputManager 是不合理的，因为没有处理多窗口问题，只是刚好这里只有一个窗口，先这么写
                        InkWindow.X11DeviceInputManager.TryReadEvents();
                    }
                }
            }
            finally
            {
                /*
                 bing:
                   如果不调用 `XFreeEventData`，会导致一些潜在问题和资源泄漏。让我详细解释一下：
                
                   - **资源泄漏**：`XGetEventData` 函数会分配内存来存储事件数据。如果不调用 `XFreeEventData` 来释放这些内存，会导致内存泄漏。这可能会在长时间运行的应用程序中累积，最终导致内存耗尽或应用程序崩溃。
                
                   - **未定义行为**：如果不调用 `XFreeEventData`，则 `XGenericEventCookie` 的数据指针将保持未定义状态。这意味着您无法访问事件数据，从而可能导致应用程序中的错误或不一致性。
                
                   - **性能问题**：如果不释放事件数据，系统可能会在内部维护大量未释放的内存块，从而影响性能。
                
                   因此，为了避免这些问题，务必在使用 `XGetEventData` 获取事件数据后调用 `XFreeEventData` 来释放内存。这是良好的编程实践，有助于确保应用程序的稳定性和性能。
                 */
                XFreeEventData(X11Info.Display, data);
            }
        }

        base.DispatchEvent(@event);
    }

    public void ReShow()
    {
        //StaticDebugLogger.WriteLine($"[{nameof(X11InkProvider)}] ReShow");
        InkWindow.ReShow();
    }

    //public void Hide()
    //{
    //    //StaticDebugLogger.WriteLine($"[{nameof(X11InkProvider)}] Hide");
    //    InkWindow.Hide();
    //}

    public void TryEnqueue(Action action) => X11PlatformThreading.TryEnqueue(action, InkWindow.X11InkWindowIntPtr);

    public Task InvokeAsync(Action action) => X11PlatformThreading.InvokeAsync(action, InkWindow.X11InkWindowIntPtr);
}