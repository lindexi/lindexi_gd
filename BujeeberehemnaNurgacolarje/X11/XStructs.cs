// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software",, to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Copyright (c) 2004 Novell, Inc.
//
// Authors:
//	Peter Bartok	pbartok@novell.com
//


// NOT COMPLETE

using System;
using System.ComponentModel;
using System.Collections;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable IdentifierTypo
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable CommentTypo
// ReSharper disable ArrangeThisQualifier
// ReSharper disable NotAccessedField.Global
#pragma warning disable 649

namespace BlankX11App.X11;
[StructLayout(LayoutKind.Sequential)]
internal struct XAnyEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint window;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XKeyEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint window;
    internal nint root;
    internal nint subwindow;
    internal nint time;
    internal int x;
    internal int y;
    internal int x_root;
    internal int y_root;
    internal XModifierMask state;
    internal int keycode;
    internal int same_screen;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XButtonEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint window;
    internal nint root;
    internal nint subwindow;
    internal nint time;
    internal int x;
    internal int y;
    internal int x_root;
    internal int y_root;
    internal XModifierMask state;
    internal int button;
    internal int same_screen;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XMotionEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint window;
    internal nint root;
    internal nint subwindow;
    internal nint time;
    internal int x;
    internal int y;
    internal int x_root;
    internal int y_root;
    internal XModifierMask state;
    internal byte is_hint;
    internal int same_screen;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XCrossingEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint window;
    internal nint root;
    internal nint subwindow;
    internal nint time;
    internal int x;
    internal int y;
    internal int x_root;
    internal int y_root;
    internal NotifyMode mode;
    internal NotifyDetail detail;
    internal int same_screen;
    internal int focus;
    internal XModifierMask state;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XFocusChangeEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint window;
    internal int mode;
    internal NotifyDetail detail;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XKeymapEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint window;
    internal byte key_vector0;
    internal byte key_vector1;
    internal byte key_vector2;
    internal byte key_vector3;
    internal byte key_vector4;
    internal byte key_vector5;
    internal byte key_vector6;
    internal byte key_vector7;
    internal byte key_vector8;
    internal byte key_vector9;
    internal byte key_vector10;
    internal byte key_vector11;
    internal byte key_vector12;
    internal byte key_vector13;
    internal byte key_vector14;
    internal byte key_vector15;
    internal byte key_vector16;
    internal byte key_vector17;
    internal byte key_vector18;
    internal byte key_vector19;
    internal byte key_vector20;
    internal byte key_vector21;
    internal byte key_vector22;
    internal byte key_vector23;
    internal byte key_vector24;
    internal byte key_vector25;
    internal byte key_vector26;
    internal byte key_vector27;
    internal byte key_vector28;
    internal byte key_vector29;
    internal byte key_vector30;
    internal byte key_vector31;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XExposeEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint window;
    internal int x;
    internal int y;
    internal int width;
    internal int height;
    internal int count;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XGraphicsExposeEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint drawable;
    internal int x;
    internal int y;
    internal int width;
    internal int height;
    internal int count;
    internal int major_code;
    internal int minor_code;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XNoExposeEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint drawable;
    internal int major_code;
    internal int minor_code;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XVisibilityEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint window;
    internal int state;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XCreateWindowEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint parent;
    internal nint window;
    internal int x;
    internal int y;
    internal int width;
    internal int height;
    internal int border_width;
    internal int override_redirect;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XDestroyWindowEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint xevent;
    internal nint window;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XUnmapEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint xevent;
    internal nint window;
    internal int from_configure;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XMapEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint xevent;
    internal nint window;
    internal int override_redirect;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XMapRequestEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint parent;
    internal nint window;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XReparentEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint xevent;
    internal nint window;
    internal nint parent;
    internal int x;
    internal int y;
    internal int override_redirect;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XConfigureEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint xevent;
    internal nint window;
    internal int x;
    internal int y;
    internal int width;
    internal int height;
    internal int border_width;
    internal nint above;
    internal int override_redirect;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XGravityEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint xevent;
    internal nint window;
    internal int x;
    internal int y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XResizeRequestEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint window;
    internal int width;
    internal int height;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XConfigureRequestEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint parent;
    internal nint window;
    internal int x;
    internal int y;
    internal int width;
    internal int height;
    internal int border_width;
    internal nint above;
    internal int detail;
    internal nint value_mask;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XCirculateEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint xevent;
    internal nint window;
    internal int place;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XCirculateRequestEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint parent;
    internal nint window;
    internal int place;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XPropertyEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint window;
    internal nint atom;
    internal nint time;
    internal int state;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XSelectionClearEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint window;
    internal nint selection;
    internal nint time;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XSelectionRequestEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint owner;
    internal nint requestor;
    internal nint selection;
    internal nint target;
    internal nint property;
    internal nint time;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XSelectionEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint requestor;
    internal nint selection;
    internal nint target;
    internal nint property;
    internal nint time;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XColormapEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint window;
    internal nint colormap;
    internal int c_new;
    internal int state;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XClientMessageEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint window;
    internal nint message_type;
    internal int format;
    internal nint ptr1;
    internal nint ptr2;
    internal nint ptr3;
    internal nint ptr4;
    internal nint ptr5;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XMappingEvent
{
    internal XEventName type;
    internal nint serial;
    internal int send_event;
    internal nint display;
    internal nint window;
    internal int request;
    internal int first_keycode;
    internal int count;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XErrorEvent
{
    internal XEventName type;
    internal nint display;
    internal nint resourceid;
    internal nint serial;
    internal byte error_code;
    internal XRequest request_code;
    internal byte minor_code;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XEventPad
{
    internal nint pad0;
    internal nint pad1;
    internal nint pad2;
    internal nint pad3;
    internal nint pad4;
    internal nint pad5;
    internal nint pad6;
    internal nint pad7;
    internal nint pad8;
    internal nint pad9;
    internal nint pad10;
    internal nint pad11;
    internal nint pad12;
    internal nint pad13;
    internal nint pad14;
    internal nint pad15;
    internal nint pad16;
    internal nint pad17;
    internal nint pad18;
    internal nint pad19;
    internal nint pad20;
    internal nint pad21;
    internal nint pad22;
    internal nint pad23;
    internal nint pad24;
    internal nint pad25;
    internal nint pad26;
    internal nint pad27;
    internal nint pad28;
    internal nint pad29;
    internal nint pad30;
    internal nint pad31;
    internal nint pad32;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XGenericEventCookie
{
    internal int type; /* of event. Always GenericEvent */
    internal nint serial; /* # of last request processed */
    internal int send_event; /* true if from SendEvent request */
    internal nint display; /* Display the event was read from */
    internal int extension; /* major opcode of extension that caused the event */
    internal int evtype; /* actual event type. */
    internal uint cookie;
    internal void* data;

    public T GetEvent<T>() where T : unmanaged
    {
        if (data == null)
            throw new InvalidOperationException();
        return Unsafe.ReadUnaligned<T>(data);
    }
}

[StructLayout(LayoutKind.Explicit)]
internal struct XEvent
{
    [FieldOffset(0)] internal XEventName type;
    [FieldOffset(0)] internal XAnyEvent AnyEvent;
    [FieldOffset(0)] internal XKeyEvent KeyEvent;
    [FieldOffset(0)] internal XButtonEvent ButtonEvent;
    [FieldOffset(0)] internal XMotionEvent MotionEvent;
    [FieldOffset(0)] internal XCrossingEvent CrossingEvent;
    [FieldOffset(0)] internal XFocusChangeEvent FocusChangeEvent;
    [FieldOffset(0)] internal XExposeEvent ExposeEvent;
    [FieldOffset(0)] internal XGraphicsExposeEvent GraphicsExposeEvent;
    [FieldOffset(0)] internal XNoExposeEvent NoExposeEvent;
    [FieldOffset(0)] internal XVisibilityEvent VisibilityEvent;
    [FieldOffset(0)] internal XCreateWindowEvent CreateWindowEvent;
    [FieldOffset(0)] internal XDestroyWindowEvent DestroyWindowEvent;
    [FieldOffset(0)] internal XUnmapEvent UnmapEvent;
    [FieldOffset(0)] internal XMapEvent MapEvent;
    [FieldOffset(0)] internal XMapRequestEvent MapRequestEvent;
    [FieldOffset(0)] internal XReparentEvent ReparentEvent;
    [FieldOffset(0)] internal XConfigureEvent ConfigureEvent;
    [FieldOffset(0)] internal XGravityEvent GravityEvent;
    [FieldOffset(0)] internal XResizeRequestEvent ResizeRequestEvent;
    [FieldOffset(0)] internal XConfigureRequestEvent ConfigureRequestEvent;
    [FieldOffset(0)] internal XCirculateEvent CirculateEvent;
    [FieldOffset(0)] internal XCirculateRequestEvent CirculateRequestEvent;
    [FieldOffset(0)] internal XPropertyEvent PropertyEvent;
    [FieldOffset(0)] internal XSelectionClearEvent SelectionClearEvent;
    [FieldOffset(0)] internal XSelectionRequestEvent SelectionRequestEvent;
    [FieldOffset(0)] internal XSelectionEvent SelectionEvent;
    [FieldOffset(0)] internal XColormapEvent ColormapEvent;
    [FieldOffset(0)] internal XClientMessageEvent ClientMessageEvent;
    [FieldOffset(0)] internal XMappingEvent MappingEvent;
    [FieldOffset(0)] internal XErrorEvent ErrorEvent;
    [FieldOffset(0)] internal XKeymapEvent KeymapEvent;
    [FieldOffset(0)] internal XGenericEventCookie GenericEventCookie;

    //[MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst=24)]
    //[ FieldOffset(0) ] internal int[] pad;
    [FieldOffset(0)] internal XEventPad Pad;
    public override string ToString()
    {
        switch (type)
        {
            case XEventName.ButtonPress:
            case XEventName.ButtonRelease:
                return ToString(ButtonEvent);
            case XEventName.CirculateNotify:
            case XEventName.CirculateRequest:
                return ToString(CirculateEvent);
            case XEventName.ClientMessage:
                return ToString(ClientMessageEvent);
            case XEventName.ColormapNotify:
                return ToString(ColormapEvent);
            case XEventName.ConfigureNotify:
                return ToString(ConfigureEvent);
            case XEventName.ConfigureRequest:
                return ToString(ConfigureRequestEvent);
            case XEventName.CreateNotify:
                return ToString(CreateWindowEvent);
            case XEventName.DestroyNotify:
                return ToString(DestroyWindowEvent);
            case XEventName.Expose:
                return ToString(ExposeEvent);
            case XEventName.FocusIn:
            case XEventName.FocusOut:
                return ToString(FocusChangeEvent);
            case XEventName.GraphicsExpose:
                return ToString(GraphicsExposeEvent);
            case XEventName.GravityNotify:
                return ToString(GravityEvent);
            case XEventName.KeymapNotify:
                return ToString(KeymapEvent);
            case XEventName.MapNotify:
                return ToString(MapEvent);
            case XEventName.MappingNotify:
                return ToString(MappingEvent);
            case XEventName.MapRequest:
                return ToString(MapRequestEvent);
            case XEventName.MotionNotify:
                return ToString(MotionEvent);
            case XEventName.NoExpose:
                return ToString(NoExposeEvent);
            case XEventName.PropertyNotify:
                return ToString(PropertyEvent);
            case XEventName.ReparentNotify:
                return ToString(ReparentEvent);
            case XEventName.ResizeRequest:
                return ToString(ResizeRequestEvent);
            case XEventName.SelectionClear:
                return ToString(SelectionClearEvent);
            case XEventName.SelectionNotify:
                return ToString(SelectionEvent);
            case XEventName.SelectionRequest:
                return ToString(SelectionRequestEvent);
            case XEventName.UnmapNotify:
                return ToString(UnmapEvent);
            case XEventName.VisibilityNotify:
                return ToString(VisibilityEvent);
            case XEventName.EnterNotify:
            case XEventName.LeaveNotify:
                return ToString(CrossingEvent);
            default:
                return type.ToString();
        }
    }

    public static string ToString(object ev)
    {
        string result = string.Empty;
        Type type = ev.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        for (int i = 0; i < fields.Length; i++)
        {
            if (!string.IsNullOrEmpty(result))
            {
                result += ", ";
            }
            object value = fields[i].GetValue(ev);
            result += fields[i].Name + "=" + (value == null ? "<null>" : value.ToString());
        }
        return type.Name + " (" + result + ")";
    }
}

internal enum XEventName
{
    KeyPress = 2,
    KeyRelease = 3,
    ButtonPress = 4,
    ButtonRelease = 5,
    MotionNotify = 6,
    EnterNotify = 7,
    LeaveNotify = 8,
    FocusIn = 9,
    FocusOut = 10,
    KeymapNotify = 11,
    Expose = 12,
    GraphicsExpose = 13,
    NoExpose = 14,
    VisibilityNotify = 15,
    CreateNotify = 16,
    DestroyNotify = 17,
    UnmapNotify = 18,
    MapNotify = 19,
    MapRequest = 20,
    ReparentNotify = 21,
    ConfigureNotify = 22,
    ConfigureRequest = 23,
    GravityNotify = 24,
    ResizeRequest = 25,
    CirculateNotify = 26,
    CirculateRequest = 27,
    PropertyNotify = 28,
    SelectionClear = 29,
    SelectionRequest = 30,
    SelectionNotify = 31,
    ColormapNotify = 32,
    ClientMessage = 33,
    MappingNotify = 34,
    GenericEvent = 35,
    LASTEvent
}

internal enum XRequest : byte
{
    X_CreateWindow = 1,
    X_ChangeWindowAttributes = 2,
    X_GetWindowAttributes = 3,
    X_DestroyWindow = 4,
    X_DestroySubwindows = 5,
    X_ChangeSaveSet = 6,
    X_ReparentWindow = 7,
    X_MapWindow = 8,
    X_MapSubwindows = 9,
    X_UnmapWindow = 10,
    X_UnmapSubwindows = 11,
    X_ConfigureWindow = 12,
    X_CirculateWindow = 13,
    X_GetGeometry = 14,
    X_QueryTree = 15,
    X_InternAtom = 16,
    X_GetAtomName = 17,
    X_ChangeProperty = 18,
    X_DeleteProperty = 19,
    X_GetProperty = 20,
    X_ListProperties = 21,
    X_SetSelectionOwner = 22,
    X_GetSelectionOwner = 23,
    X_ConvertSelection = 24,
    X_SendEvent = 25,
    X_GrabPointer = 26,
    X_UngrabPointer = 27,
    X_GrabButton = 28,
    X_UngrabButton = 29,
    X_ChangeActivePointerGrab = 30,
    X_GrabKeyboard = 31,
    X_UngrabKeyboard = 32,
    X_GrabKey = 33,
    X_UngrabKey = 34,
    X_AllowEvents = 35,
    X_GrabServer = 36,
    X_UngrabServer = 37,
    X_QueryPointer = 38,
    X_GetMotionEvents = 39,
    X_TranslateCoords = 40,
    X_WarpPointer = 41,
    X_SetInputFocus = 42,
    X_GetInputFocus = 43,
    X_QueryKeymap = 44,
    X_OpenFont = 45,
    X_CloseFont = 46,
    X_QueryFont = 47,
    X_QueryTextExtents = 48,
    X_ListFonts = 49,
    X_ListFontsWithInfo = 50,
    X_SetFontPath = 51,
    X_GetFontPath = 52,
    X_CreatePixmap = 53,
    X_FreePixmap = 54,
    X_CreateGC = 55,
    X_ChangeGC = 56,
    X_CopyGC = 57,
    X_SetDashes = 58,
    X_SetClipRectangles = 59,
    X_FreeGC = 60,
    X_ClearArea = 61,
    X_CopyArea = 62,
    X_CopyPlane = 63,
    X_PolyPoint = 64,
    X_PolyLine = 65,
    X_PolySegment = 66,
    X_PolyRectangle = 67,
    X_PolyArc = 68,
    X_FillPoly = 69,
    X_PolyFillRectangle = 70,
    X_PolyFillArc = 71,
    X_PutImage = 72,
    X_GetImage = 73,
    X_PolyText8 = 74,
    X_PolyText16 = 75,
    X_ImageText8 = 76,
    X_ImageText16 = 77,
    X_CreateColormap = 78,
    X_FreeColormap = 79,
    X_CopyColormapAndFree = 80,
    X_InstallColormap = 81,
    X_UninstallColormap = 82,
    X_ListInstalledColormaps = 83,
    X_AllocColor = 84,
    X_AllocNamedColor = 85,
    X_AllocColorCells = 86,
    X_AllocColorPlanes = 87,
    X_FreeColors = 88,
    X_StoreColors = 89,
    X_StoreNamedColor = 90,
    X_QueryColors = 91,
    X_LookupColor = 92,
    X_CreateCursor = 93,
    X_CreateGlyphCursor = 94,
    X_FreeCursor = 95,
    X_RecolorCursor = 96,
    X_QueryBestSize = 97,
    X_QueryExtension = 98,
    X_ListExtensions = 99,
    X_ChangeKeyboardMapping = 100,
    X_GetKeyboardMapping = 101,
    X_ChangeKeyboardControl = 102,
    X_GetKeyboardControl = 103,
    X_Bell = 104,
    X_ChangePointerControl = 105,
    X_GetPointerControl = 106,
    X_SetScreenSaver = 107,
    X_GetScreenSaver = 108,
    X_ChangeHosts = 109,
    X_ListHosts = 110,
    X_SetAccessControl = 111,
    X_SetCloseDownMode = 112,
    X_KillClient = 113,
    X_RotateProperties = 114,
    X_ForceScreenSaver = 115,
    X_SetPointerMapping = 116,
    X_GetPointerMapping = 117,
    X_SetModifierMapping = 118,
    X_GetModifierMapping = 119,
    X_NoOperation = 127
}

[StructLayout(LayoutKind.Sequential)]
internal struct XVisualInfo
{
    internal nint visual;
    internal nint visualid;
    internal int screen;
    internal uint depth;
    internal int klass;
    internal nint red_mask;
    internal nint green_mask;
    internal nint blue_mask;
    internal int colormap_size;
    internal int bits_per_rgb;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XSetWindowAttributes
{
    internal nint background_pixmap;
    internal nint background_pixel;
    internal nint border_pixmap;
    internal nint border_pixel;
    internal Gravity bit_gravity;
    internal Gravity win_gravity;
    internal int backing_store;
    internal nint backing_planes;
    internal nint backing_pixel;
    internal int save_under;
    internal nint event_mask;
    internal nint do_not_propagate_mask;
    internal int override_redirect;
    internal nint colormap;
    internal nint cursor;
}
