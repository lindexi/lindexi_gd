using CPF.Linux;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static CPF.Linux.XLib; // CPF: https://gitee.com/csharpui/CPF

namespace RaynilequKuharrurnudoqer;

internal class X11WindowNativeInterop
{
    public X11WindowNativeInterop(IntPtr display, IntPtr x11WindowIntPtr, IntPtr rootWindow)
    {
        Display = display;
        X11WindowIntPtr = x11WindowIntPtr;
        RootWindow = rootWindow;
    }

    public IntPtr Display { get; }

    public IntPtr X11WindowIntPtr { get; }
    public IntPtr RootWindow { get; }

    public void ShowActive()
    {
        XMapWindow(Display, X11WindowIntPtr);
        //XFlush(Display);
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
        var wmState = XInternAtom(Display, "_NET_WM_STATE", true);

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
        XSendEvent(Display, RootWindow, false,
            new IntPtr((int) (EventMask.SubstructureRedirectMask | EventMask.SubstructureNotifyMask)), ref xev);
    }

    public void EnterFullScreen(bool topmost)
    {
        // 下面是进入全屏
        var display = Display;
        var hintsPropertyAtom = HintsPropertyAtom;
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
            SendNetWMMessage(WMStateAtom, new IntPtr(1), topmostAtom);
        }
    }

    public IntPtr HintsPropertyAtom => GetAtom(ref _hintsPropertyAtom, "_MOTIF_WM_HINTS");
    private IntPtr _hintsPropertyAtom;
    public IntPtr WMStateAtom => GetAtom(ref _wmStateAtom, "_NET_WM_STATE");
    private IntPtr _wmStateAtom;

    private IntPtr GetAtom(ref IntPtr atom, string atomName)
    {
        if (atom == IntPtr.Zero)
        {
            atom = GetAtom(atomName);
        }

        return atom;
    }

    private IntPtr GetAtom(string atomName)
    {
        return XInternAtom(Display, atomName, true);
    }
}
