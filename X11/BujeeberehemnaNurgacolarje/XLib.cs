using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace CPF.Linux
{
    public unsafe static class XLib
    {
        const string libX11 = "libX11.so.6";
        const string libX11Randr = "libXrandr.so.2";
        const string libX11Ext = "libXext.so.6";
        const string libXInput = "libXi.so.6";
        const string libXcomposite = "libXcomposite.so.1";

        [DllImport("libXcomposite.so.1", EntryPoint = "XCompositeQueryExtension")]
        public static extern int XCompositeQueryExtension(IntPtr display, out int eventBase, out int errorBase);
        [DllImport("libXcomposite.so.1", EntryPoint = "XCompositeRedirectSubwindows")]
        public static extern void XCompositeRedirectSubwindows(IntPtr display, IntPtr overlayWindow, int update);

        [DllImport(libX11)]
        public static extern IntPtr XCreateRegion();
        [DllImport("libXext.so")]
        public static extern void XShapeCombineRegion(IntPtr display, IntPtr dest, int destKind, int xOff, int yOff, IntPtr region, int op);

        [DllImport(libX11)]
        public static extern IntPtr XOpenDisplay(IntPtr display);

        [DllImport(libX11)]
        public static extern int XDisplayWidth(IntPtr display, int screen_number);
        [DllImport(libX11)]
        public static extern int XDisplayHeight(IntPtr display, int screen_number);

        [DllImport(libX11)]
        public static extern int XCloseDisplay(IntPtr display);

        [DllImport(libX11)]
        public static extern IntPtr XSynchronize(IntPtr display, bool onoff);

        [DllImport(libX11)]
        public static extern IntPtr XCreateWindow(IntPtr display, IntPtr parent, int x, int y, int width, int height,
            int border_width, int depth, int xclass, IntPtr visual, UIntPtr valuemask,
            ref XSetWindowAttributes attributes);

        [DllImport(libX11)]
        public static extern IntPtr XCreateWindow(IntPtr display, IntPtr parent, int x, int y, uint width,
            uint height, uint border_width, int depth, uint @class, IntPtr visual, ulong valuemask,
              ref XSetWindowAttributes attributes);

        [DllImport(libX11)]
        public static extern Status XChangeWindowAttributes(IntPtr display, IntPtr window, ulong valuemask, ref XSetWindowAttributes attributes);

        [DllImport(libX11)]
        public static extern IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, int width,
            int height, int border_width, IntPtr border, IntPtr background);

        [DllImport(libX11)]
        public static extern int XMapWindow(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern int XUnmapWindow(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern int XMapSubindows(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern int XUnmapSubwindows(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern IntPtr XRootWindow(IntPtr display, int screen_number);
        [DllImport(libX11)]
        public static extern IntPtr XDefaultRootWindow(IntPtr display);

        [DllImport(libX11)]
        public static extern IntPtr XNextEvent(IntPtr display, out XEvent xevent);

        [DllImport(libX11)]
        public static extern int XConnectionNumber(IntPtr diplay);

        [DllImport(libX11)]
        public static extern int XPending(IntPtr diplay);

        [DllImport(libX11)]
        public static extern int XEventsQueued(IntPtr display, int mode);

        [DllImport(libX11)]
        public static extern IntPtr XSelectInput(IntPtr display, IntPtr window, IntPtr mask);

        [DllImport(libX11)]
        public static extern int XDestroyWindow(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern int XReparentWindow(IntPtr display, IntPtr window, IntPtr parent, int x, int y);
        [DllImport(libX11)]
        public static extern void XMoveWindow(IntPtr display, IntPtr window, int x, int y);

        [DllImport(libX11)]
        public static extern int XMoveResizeWindow(IntPtr display, IntPtr window, int x, int y, int width, int height);

        [DllImport(libX11)]
        public static extern int XResizeWindow(IntPtr display, IntPtr window, int width, int height);

        [DllImport(libX11)]
        public static extern Status XGetWindowAttributes(IntPtr display, IntPtr window, ref XWindowAttributes attributes);

        [DllImport(libX11)]
        public static extern int XFlush(IntPtr display);

        [DllImport(libX11)]
        public static extern int XSetWMName(IntPtr display, IntPtr window, ref XTextProperty text_prop);

        [DllImport(libX11)]
        public static extern void XSetWMIconName(IntPtr display, IntPtr window, ref XTextProperty text_prop);

        [DllImport(libX11)]
        public static extern int XStoreName(IntPtr display, IntPtr window, string window_name);

        [DllImport(libX11)]
        public static extern int XSetIconName(IntPtr display, IntPtr window, string window_name);

        [DllImport(libX11)]
        public static extern int XFetchName(IntPtr display, IntPtr window, ref IntPtr window_name);

        [DllImport(libX11)]
        public static extern Status XSendEvent(IntPtr display, IntPtr window, bool propagate, IntPtr event_mask,
            ref XEvent send_event);

        [DllImport(libX11)]
        public static extern int XQueryTree(IntPtr display, IntPtr window, out IntPtr root_return,
            out IntPtr parent_return, out IntPtr children_return, out int nchildren_return);

        [DllImport(libX11)]
        public static extern int XFree(IntPtr data);

        [DllImport(libX11)]
        public static extern int XRaiseWindow(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern uint XLowerWindow(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern uint XConfigureWindow(IntPtr display, IntPtr window, ChangeWindowFlags value_mask,
            ref XWindowChanges values);

        public static uint XConfigureResizeWindow(IntPtr display, IntPtr window, PixelSize size)
            => XConfigureResizeWindow(display, window, size.Width, size.Height);

        public static uint XConfigureResizeWindow(IntPtr display, IntPtr window, int width, int height)
        {
            var changes = new XWindowChanges
            {
                width = width,
                height = height
            };

            return XConfigureWindow(display, window, ChangeWindowFlags.CWHeight | ChangeWindowFlags.CWWidth,
                ref changes);
        }

        [DllImport(libX11)]
        public static extern IntPtr XInternAtom(IntPtr display,[MarshalAs(UnmanagedType.LPStr)] string atom_name, bool only_if_exists);

        [DllImport(libX11)]
        public static extern int XInternAtoms(IntPtr display, string[] atom_names, int atom_count, bool only_if_exists,
            IntPtr[] atoms);

        [DllImport(libX11)]
        public static extern IntPtr XGetAtomName(IntPtr display, IntPtr atom);

        public static string? GetAtomName(IntPtr display, IntPtr atom)
        {
            var ptr = XGetAtomName(display, atom);
            if (ptr == IntPtr.Zero)
                return null;
            var s = Marshal.PtrToStringAnsi(ptr);
            XFree(ptr);
            return s;
        }

        [DllImport(libX11)]
        public static extern int XSetWMProtocols(IntPtr display, IntPtr window, IntPtr[] protocols, int count);

        [DllImport(libX11)]
        public static extern int XGrabPointer(IntPtr display, IntPtr window, bool owner_events, EventMask event_mask,
            GrabMode pointer_mode, GrabMode keyboard_mode, IntPtr confine_to, IntPtr cursor, IntPtr timestamp);

        [DllImport(libX11)]
        public static extern int XUngrabPointer(IntPtr display, IntPtr timestamp);

        [DllImport(libX11)]
        public static extern bool XQueryPointer(IntPtr display, IntPtr window, out IntPtr root, out IntPtr child,
            out int root_x, out int root_y, out int win_x, out int win_y, out int keys_buttons);

        [DllImport(libX11)]
        public static extern bool XTranslateCoordinates(IntPtr display, IntPtr src_w, IntPtr dest_w, int src_x,
            int src_y, out int intdest_x_return, out int dest_y_return, out IntPtr child_return);

        [DllImport(libX11)]
        public static extern bool XGetGeometry(IntPtr display, IntPtr window, out IntPtr root, out int x, out int y,
            out int width, out int height, out int border_width, out int depth);

        [DllImport(libX11)]
        public static extern bool XGetGeometry(IntPtr display, IntPtr window, IntPtr root, out int x, out int y,
            out int width, out int height, IntPtr border_width, IntPtr depth);

        [DllImport(libX11)]
        public static extern bool XGetGeometry(IntPtr display, IntPtr window, IntPtr root, out int x, out int y,
            IntPtr width, IntPtr height, IntPtr border_width, IntPtr depth);

        [DllImport(libX11)]
        public static extern bool XGetGeometry(IntPtr display, IntPtr window, IntPtr root, IntPtr x, IntPtr y,
            out int width, out int height, IntPtr border_width, IntPtr depth);

        [DllImport(libX11)]
        public static extern uint XWarpPointer(IntPtr display, IntPtr src_w, IntPtr dest_w, int src_x, int src_y,
            uint src_width, uint src_height, int dest_x, int dest_y);

        [DllImport(libX11)]
        public static extern int XClearWindow(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern int XClearArea(IntPtr display, IntPtr window, int x, int y, int width, int height,
            bool exposures);

        // Colormaps
        [DllImport(libX11)]
        public static extern IntPtr XDefaultScreenOfDisplay(IntPtr display);

        [DllImport(libX11)]
        public static extern int XScreenNumberOfScreen(IntPtr display, IntPtr Screen);

        [DllImport(libX11)]
        public static extern IntPtr XDefaultVisual(IntPtr display, int screen_number);

        [DllImport(libX11)]
        public static extern uint XDefaultDepth(IntPtr display, int screen_number);

        [DllImport(libX11)]
        public static extern int XDefaultScreen(IntPtr display);

        [DllImport(libX11)]
        public static extern IntPtr XDefaultColormap(IntPtr display, int screen_number);

        [DllImport(libX11)]
        public static extern int XLookupColor(IntPtr display, IntPtr Colormap, string Coloranem,
            ref XColor exact_def_color, ref XColor screen_def_color);

        [DllImport(libX11)]
        public static extern int XAllocColor(IntPtr display, IntPtr Colormap, ref XColor colorcell_def);

        [DllImport(libX11)]
        public static extern int XSetTransientForHint(IntPtr display, IntPtr window, IntPtr parent);

        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, ref MotifWmHints data, int nelements);

        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, ref uint value, int nelements);

        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, ref IntPtr value, int nelements);

        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, uint[] data, int nelements);

        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, int[] data, int nelements);

        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, IntPtr[] data, int nelements);
        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, void* data, int nelements);

        [DllImport(libX11)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, IntPtr atoms, int nelements);

        [DllImport(libX11, CharSet = CharSet.Ansi)]
        public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
            int format, PropertyMode mode, string text, int text_length);

        [DllImport(libX11)]
        public static extern int XDeleteProperty(IntPtr display, IntPtr window, IntPtr property);

        // Drawing
        [DllImport(libX11)]
        public static extern IntPtr XCreateGC(IntPtr display, IntPtr window, IntPtr valuemask, ref XGCValues values);

        [DllImport(libX11)]
        public static extern int XFreeGC(IntPtr display, IntPtr gc);

        [DllImport(libX11)]
        public static extern int XSetFunction(IntPtr display, IntPtr gc, GXFunction function);

        [DllImport(libX11)]
        internal static extern int XSetLineAttributes(IntPtr display, IntPtr gc, int line_width, GCLineStyle line_style,
            GCCapStyle cap_style, GCJoinStyle join_style);

        [DllImport(libX11)]
        public static extern int XDrawLine(IntPtr display, IntPtr drawable, IntPtr gc, int x1, int y1, int x2, int y2);

        [DllImport(libX11)]
        public static extern int XDrawRectangle(IntPtr display, IntPtr drawable, IntPtr gc, int x1, int y1, int width,
            int height);

        [DllImport(libX11)]
        public static extern int XFillRectangle(IntPtr display, IntPtr drawable, IntPtr gc, int x1, int y1, int width,
            int height);

        [DllImport(libX11)]
        public static extern int XSetWindowBackground(IntPtr display, IntPtr window, IntPtr background);

        [DllImport(libX11)]
        public static extern int XCopyArea(IntPtr display, IntPtr src, IntPtr dest, IntPtr gc, int src_x, int src_y,
            int width, int height, int dest_x, int dest_y);

        [DllImport(libX11)]
        public static extern int XGetWindowProperty(IntPtr display, IntPtr window, IntPtr atom, IntPtr long_offset,
            IntPtr long_length, bool delete, IntPtr req_type, out IntPtr actual_type, out int actual_format,
            out IntPtr nitems, out IntPtr bytes_after, out IntPtr prop);

        [DllImport(libX11)]
        public static extern int XSetInputFocus(IntPtr display, IntPtr window, RevertTo revert_to, IntPtr time);

        [DllImport(libX11)]
        public static extern int XIconifyWindow(IntPtr display, IntPtr window, int screen_number);

        [DllImport(libX11)]
        public static extern int XDefineCursor(IntPtr display, IntPtr window, IntPtr cursor);

        [DllImport(libX11)]
        public static extern int XUndefineCursor(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern int XFreeCursor(IntPtr display, IntPtr cursor);

        [DllImport(libX11)]
        public static extern IntPtr XCreateFontCursor(IntPtr display, CursorFontShape shape);

        [DllImport(libX11)]
        public static extern IntPtr XCreatePixmapCursor(IntPtr display, IntPtr source, IntPtr mask,
            ref XColor foreground_color, ref XColor background_color, int x_hot, int y_hot);

        [DllImport(libX11)]
        public static extern IntPtr XCreatePixmapFromBitmapData(IntPtr display, IntPtr drawable, byte[] data, int width,
            int height, IntPtr fg, IntPtr bg, int depth);

        [DllImport(libX11)]
        public static extern IntPtr XCreatePixmap(IntPtr display, IntPtr d, int width, int height, int depth);

        [DllImport(libX11)]
        public static extern IntPtr XFreePixmap(IntPtr display, IntPtr pixmap);

        [DllImport(libX11)]
        public static extern int XQueryBestCursor(IntPtr display, IntPtr drawable, int width, int height,
            out int best_width, out int best_height);

        [DllImport(libX11)]
        public static extern IntPtr XWhitePixel(IntPtr display, int screen_no);

        [DllImport(libX11)]
        public static extern IntPtr XBlackPixel(IntPtr display, int screen_no);

        [DllImport(libX11)]
        public static extern void XGrabServer(IntPtr display);

        [DllImport(libX11)]
        public static extern void XUngrabServer(IntPtr display);

        [DllImport(libX11)]
        public static extern void XGetWMNormalHints(IntPtr display, IntPtr window, ref XSizeHints hints,
            out IntPtr supplied_return);

        [DllImport(libX11)]
        public static extern void XSetWMNormalHints(IntPtr display, IntPtr window, ref XSizeHints hints);

        [DllImport(libX11)]
        public static extern void XSetZoomHints(IntPtr display, IntPtr window, ref XSizeHints hints);

        [DllImport(libX11)]
        public static extern void XSetWMHints(IntPtr display, IntPtr window, ref XWMHints wmhints);

        [DllImport(libX11)]
        public static extern int XGetIconSizes(IntPtr display, IntPtr window, out IntPtr size_list, out int count);

        [DllImport(libX11)]
        public static extern IntPtr XSetErrorHandler(XErrorHandler error_handler);

        [DllImport(libX11)]
        public static extern IntPtr XGetErrorText(IntPtr display, byte code, StringBuilder buffer, int length);

        [DllImport(libX11)]
        public static extern int XInitThreads();

        [DllImport(libX11)]
        public static extern int XConvertSelection(IntPtr display, IntPtr selection, IntPtr target, IntPtr property,
            IntPtr requestor, IntPtr time);

        [DllImport(libX11)]
        public static extern IntPtr XGetSelectionOwner(IntPtr display, IntPtr selection);

        [DllImport(libX11)]
        public static extern int XSetSelectionOwner(IntPtr display, IntPtr selection, IntPtr owner, IntPtr time);

        [DllImport(libX11)]
        public static extern int XSetPlaneMask(IntPtr display, IntPtr gc, IntPtr mask);

        [DllImport(libX11)]
        public static extern int XSetForeground(IntPtr display, IntPtr gc, IntPtr foreground);

        [DllImport(libX11)]
        public static extern int XSetBackground(IntPtr display, IntPtr gc, IntPtr background);

        [DllImport(libX11)]
        public static extern int XBell(IntPtr display, int percent);

        [DllImport(libX11)]
        public static extern int XChangeActivePointerGrab(IntPtr display, EventMask event_mask, IntPtr cursor,
            IntPtr time);

        [DllImport(libX11)]
        public static extern bool XFilterEvent(ref XEvent xevent, IntPtr window);

        [DllImport(libX11)]
        public static extern void XkbSetDetectableAutoRepeat(IntPtr display, bool detectable, IntPtr supported);

        [DllImport(libX11)]
        public static extern void XPeekEvent(IntPtr display, out XEvent xevent);

        [DllImport(libX11)]
        public static extern void XMatchVisualInfo(IntPtr display, int screen, int depth, int klass, out XVisualInfo info);

        [DllImport(libX11)]
        public static extern IntPtr XLockDisplay(IntPtr display);

        [DllImport(libX11)]
        public static extern IntPtr XUnlockDisplay(IntPtr display);

        [DllImport(libX11)]
        public static extern IntPtr XCreateGC(IntPtr display, IntPtr drawable, ulong valuemask, IntPtr values);

        [DllImport(libX11)]
        public static extern int XInitImage(ref XImage image);

        [DllImport(libX11)]
        public static extern int XDestroyImage(ref XImage image);
        [DllImport(libX11)]
        public static extern int XDestroyImage(IntPtr image);

        [DllImport(libX11)]
        public static extern int XPutImage(IntPtr display, IntPtr drawable, IntPtr gc, ref XImage image,
            int srcx, int srcy, int destx, int desty, uint width, uint height);
        [DllImport(libX11)]
        public static extern XImage* XGetSubImage(IntPtr display, IntPtr Drawable, int x, int y, uint width, uint height, int plane_mask, int format, XImage* dest_image, int dest_x, int dest_y);
        [DllImport(libX11, EntryPoint = "XGetImage")]
        public extern static IntPtr XGetImage(IntPtr display, IntPtr drawable, int src_x, int src_y, int width, int height, int pane, int format);
        [DllImport(libX11, EntryPoint = "XGetPixel")]
        public extern static int XGetPixel(IntPtr image, int x, int y);
        [DllImport(libX11)]
        public static extern int XSync(IntPtr display, bool discard);

        [DllImport(libX11)]
        public static extern IntPtr XCreateColormap(IntPtr display, IntPtr window, IntPtr visual, int create);

        public enum XLookupStatus
        {
            XBufferOverflow = -1,
            XLookupNone = 1,
            XLookupChars = 2,
            XLookupKeySym = 3,
            XLookupBoth = 4
        }

        public class LibC
        {
            public static int LC_ALL = 6;
            public static int LC_COLLATE = 3;
            public static int LC_CTYPE = 0;
        }
        [DllImport("libc")]
        public static extern string setlocale(int category, string locale);

        [DllImport("libc")]
        public static extern string setlocale(int category, IntPtr locale);

        [DllImport("libc")]
        public static extern double atof(IntPtr str);

        [DllImport(libX11)]
        public static extern IntPtr XCreateFontSet(IntPtr display, string base_font_name_list, out IntPtr missing_charset_list, out int missing_charset_count_return, ref IntPtr def_string);
        //[DllImport(libX11)]
        //public static extern IntPtr XCreateFontSet(IntPtr display, string base_font_name_list, out IntPtr missing_charset_list, out int missing_charset_count_return, out string def_string);

        [DllImport(libX11)]
        public static extern IntPtr XVaCreateNestedList(int zero, string xname, IntPtr fontSet, IntPtr z);

        [DllImport(libX11)]
        public static extern IntPtr XVaCreateNestedList(int zero, string xname, ref XRectangle rect, IntPtr z);

        [DllImport(libX11, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr XVaCreateNestedList(int dummy, IntPtr name0, XPoint value0, IntPtr terminator);
        [DllImport(libX11)]
        public static extern IntPtr XVaCreateNestedList(int dummy, IntPtr name0, XPoint value0, IntPtr name1, IntPtr value1, IntPtr terminator);
        [DllImport(libX11)]
        public static extern IntPtr XVaCreateNestedList(int dummy, string name0, XPoint value0, string name1, IntPtr value1, IntPtr terminator);

        [DllImport(libX11)]
        internal extern static int XLookupString(ref XEvent xevent, byte[] buffer, int num_bytes, out IntPtr keysym, out IntPtr status);
        [DllImport(libX11)]
        internal extern static int Xutf8LookupString(IntPtr xic, ref XEvent xevent, byte[] buffer, int num_bytes, out IntPtr keysym, out XLookupStatus status);

        //[DllImport(libX11)]
        //public static extern unsafe int XLookupString(ref XEvent xevent, void* buffer, int num_bytes, out IntPtr keysym, out IntPtr status);

        //[DllImport(libX11)]
        //public static extern unsafe int Xutf8LookupString(IntPtr xic, ref XEvent xevent, void* buffer, int num_bytes, out IntPtr keysym, out IntPtr status);

        [DllImport(libX11)]
        public static extern unsafe int XwcLookupString(IntPtr xic, ref XEvent xevent, void* buffer, int num_bytes, out IntPtr keysym, out IntPtr status);

        //[DllImport(libX11)]
        //public static extern unsafe int XmbLookupString(IntPtr xic, ref XKeyEvent xevent, void* buffer, int num_bytes, out X11Key keysym, out LookupStatus status);

        [DllImport(libX11)]
        public static extern unsafe IntPtr XKeycodeToKeysym(IntPtr display, int keycode, int index);

        //[DllImport(libX11)]
        //public static extern string XSetLocaleModifiers(IntPtr modifiers);

        [DllImport(libX11)]
        public static extern IntPtr XSetLocaleModifiers(string mods);

        [DllImport(libX11)]
        public static extern IntPtr XOpenIM(IntPtr display, IntPtr rdb, IntPtr res_name, IntPtr res_class);

        [DllImport(libX11)]
        public static extern string XLocaleOfIM(IntPtr xim);

        [DllImport(libX11)]
        public static extern IntPtr XCreateIC(IntPtr xim, string name, XIMProperties im_style, string name2, IntPtr value2, IntPtr terminator);

        [DllImport(libX11)]
        public static extern IntPtr XCreateIC(IntPtr xim, string name, XIMProperties im_style, string name2, IntPtr value2, string name3, IntPtr value3, IntPtr terminator);

        [DllImport(libX11)]
        public static extern IntPtr XCreateIC(IntPtr xim, string name, XIMProperties im_style, string name2, IntPtr value2, string name3, IntPtr value3, string name4, IntPtr value4, IntPtr terminator);

        [DllImport(libX11)]
        public static extern void XSetICFocus(IntPtr xic);

        [DllImport(libX11, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XSetICValues(IntPtr xic, string name, IntPtr value, IntPtr terminator);

        [DllImport(libX11, CallingConvention = CallingConvention.Cdecl)]
        public static extern string XSetICValues(IntPtr xic, __arglist);

        [DllImport(libX11, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr XGetICValues(IntPtr xic, string p1, ref XIMStyles p2);

        [DllImport(libX11, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr XGetICValues(IntPtr xic, string p1, ref long p2, IntPtr zero);

        [DllImport(libX11, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr XGetICValues(IntPtr xic, string p1, IntPtr p2, IntPtr zero);

        [DllImport(libX11)]
        public static extern string XGetIMValues(IntPtr xim, string name, out IntPtr value, IntPtr terminator);

        [DllImport(libX11)]
        public static extern string Xutf8ResetIC(IntPtr xic);

        [DllImport(libX11)]
        public static extern void XUnsetICFocus(IntPtr xic);

        [DllImport(libX11)]
        public static extern bool XSupportsLocale();

        [DllImport(libX11)]
        public static extern void XCloseIM(IntPtr xim);

        [DllImport(libX11)]
        public static extern void XDestroyIC(IntPtr xic);

        [DllImport(libX11)]
        public static extern bool XQueryExtension(IntPtr display, [MarshalAs(UnmanagedType.LPStr)] string name,
            out int majorOpcode, out int firstEvent, out int firstError);

        [DllImport(libX11)]
        public static extern bool XGetEventData(IntPtr display, void* cookie);

        [DllImport(libX11)]
        public static extern void XFreeEventData(IntPtr display, void* cookie);

        [DllImport(libX11)]
        public static extern IntPtr XResourceManagerString(IntPtr display);

        [DllImport(libX11)]
        public static extern void XrmInitialize();

        [DllImport(libX11)]
        public static extern IntPtr XrmGetStringDatabase(IntPtr data);

        [DllImport(libX11)]
        public static extern bool XrmGetResource(IntPtr db, string str_name, string str_class, ref IntPtr str_type_return, ref XrmValue value);

        [DllImport("libXss.so.1")]
        public static extern Status XScreenSaverQueryInfo(IntPtr display, IntPtr window, ref XScreenSaverInfo saver_info);

        [DllImport(libX11Randr)]
        public static extern IntPtr XRRGetScreenInfo(IntPtr dpy, IntPtr window);
        [DllImport(libX11Randr)]
        public static extern int XRRQueryExtension(IntPtr dpy,
            out int event_base_return,
            out int error_base_return);

        [DllImport(libX11Randr)]
        public static extern int XRRQueryVersion(IntPtr dpy,
            out int major_version_return,
            out int minor_version_return);

        [DllImport(libX11Randr)]
        public static extern XRRMonitorInfo* XRRGetMonitors(IntPtr dpy, IntPtr window, bool get_active, out int nmonitors);
        [DllImport(libX11Randr)]
        public static extern IntPtr* XRRListOutputProperties(IntPtr dpy, IntPtr output, out int count);

        [DllImport(libX11Randr)]
        public static extern int XRRGetOutputProperty(IntPtr dpy, IntPtr output, IntPtr atom, int offset, int length, bool _delete, bool pending, IntPtr req_type, out IntPtr actual_type, out int actual_format, out int nitems, out long bytes_after, out IntPtr prop);

        [DllImport(libX11Randr)]
        public static extern void XRRSelectInput(IntPtr dpy, IntPtr window, RandrEventMask mask);

        [DllImport(libXInput)]
        public static extern Status XIQueryVersion(IntPtr dpy, ref int major, ref int minor);

        [DllImport(libXInput)]
        public static extern IntPtr XIQueryDevice(IntPtr dpy, int deviceid, out int ndevices_return);

        [DllImport(libXInput)]
        public static extern void XIFreeDeviceInfo(XIDeviceInfo* info);

        public static void XISetMask(ref int mask, XiEventType ev)
        {
            mask |= (1 << (int)ev);
        }

        public static int XiEventMaskLen { get; } = 4;

        public static bool XIMaskIsSet(void* ptr, int shift) =>
            (((byte*)(ptr))[(shift) >> 3] & (1 << (shift & 7))) != 0;

        [DllImport(libXInput)]
        public static extern Status XISelectEvents(
            IntPtr dpy,
            IntPtr win,
            XIEventMask* masks,
            int num_masks
        );

        public static Status XiSelectEvents(IntPtr display, IntPtr window, Dictionary<int, List<XiEventType>> devices)
        {
            var masks = stackalloc int[devices.Count];
            var emasks = stackalloc XIEventMask[devices.Count];
            int c = 0;
            foreach (var d in devices)
            {
                foreach (var ev in d.Value)
                    XISetMask(ref masks[c], ev);
                emasks[c] = new XIEventMask
                {
                    Mask = &masks[c],
                    Deviceid = d.Key,
                    MaskLen = XiEventMaskLen
                };
                c++;
            }


            return XISelectEvents(display, window, emasks, devices.Count);

        }

        public struct XGeometry
        {
            public IntPtr root;
            public int x;
            public int y;
            public int width;
            public int height;
            public int bw;
            public int d;
        }

        public static bool XGetGeometry(IntPtr display, IntPtr window, out XGeometry geo)
        {
            geo = new XGeometry();
            return XGetGeometry(display, window, out geo.root, out geo.x, out geo.y, out geo.width, out geo.height,
                out geo.bw, out geo.d);
        }

        public static void QueryPointer(IntPtr display, IntPtr w, out IntPtr root, out IntPtr child,
            out int root_x, out int root_y, out int child_x, out int child_y,
            out int mask)
        {

            IntPtr c;

            XGrabServer(display);

            XQueryPointer(display, w, out root, out c,
                out root_x, out root_y, out child_x, out child_y,
                out mask);

            if (root != w)
                c = root;

            IntPtr child_last = IntPtr.Zero;
            while (c != IntPtr.Zero)
            {
                child_last = c;
                XQueryPointer(display, c, out root, out c,
                    out root_x, out root_y, out child_x, out child_y,
                    out mask);
            }
            XUngrabServer(display);
            XFlush(display);

            child = child_last;
        }

        public static (int x, int y) GetCursorPos(X11Info x11, IntPtr? handle = null)
        {
            IntPtr root;
            IntPtr child;
            int root_x;
            int root_y;
            int win_x;
            int win_y;
            int keys_buttons;



            QueryPointer(x11.Display, handle ?? x11.RootWindow, out root, out child, out root_x, out root_y, out win_x, out win_y,
                out keys_buttons);


            if (handle != null)
            {
                return (win_x, win_y);
            }
            else
            {
                return (root_x, root_y);
            }
        }

        [DllImport("libXext.so.6")]
        public extern static void XShapeCombineRectangles(IntPtr display, IntPtr window, XShapeKind dest_kind, int x_off, int y_off, XRectangle[] rectangles, int n_rects, XShapeOperation op, XOrdering ordering);
        //public static IntPtr CreateEventWindow(LinuxPlatform plat, Action<XEvent> handler)
        //{
        //    var win = XCreateSimpleWindow(plat.Display, plat.Info.DefaultRootWindow,
        //        0, 0, 1, 1, 0, IntPtr.Zero, IntPtr.Zero);
        //    plat.Windows[win] = handler;
        //    return win;
        //}


        [StructLayout(LayoutKind.Explicit)]
        public struct epoll_data
        {
            [FieldOffset(0)]
            public IntPtr ptr;
            [FieldOffset(0)]
            public int fd;
            [FieldOffset(0)]
            public uint u32;
            [FieldOffset(0)]
            public ulong u64;
        }

        public const int EPOLLIN = 1;
        public const int EPOLL_CTL_ADD = 1;
        public const int O_NONBLOCK = 2048;
        public const int O_NDELAY = (0x0004 | O_NONBLOCK);
        public const int O_DIRECT = 0x100000;

        [StructLayout(LayoutKind.Sequential)]
        public struct epoll_event
        {
            public uint events;
            public epoll_data data;
        }

        public struct Pollfd : IEquatable<Pollfd>
        {
            public int fd;
            [CLSCompliant(false)]
            public PollEvents events;
            [CLSCompliant(false)]
            public PollEvents revents;

            public override int GetHashCode()
            {
                return events.GetHashCode() ^ revents.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null || obj.GetType() != GetType())
                    return false;
                Pollfd value = (Pollfd)obj;
                return value.events == events && value.revents == revents;
            }

            public bool Equals(Pollfd value)
            {
                return value.events == events && value.revents == revents;
            }

            public static bool operator ==(Pollfd lhs, Pollfd rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(Pollfd lhs, Pollfd rhs)
            {
                return !lhs.Equals(rhs);
            }
        }
        private struct _pollfd
        {
            public int fd;
            public short events;
            public short revents;
        }
        [DllImport("libc", SetLastError = true, EntryPoint = "poll")]
        private static extern int sys_poll([In, Out] _pollfd[] ufds, uint nfds, int timeout);

        public static int poll(Pollfd[] fds, uint nfds, int timeout)
        {
            if (fds.Length < nfds)
                throw new ArgumentOutOfRangeException("fds", "Must refer to at least `nfds' elements");

            _pollfd[] send = new _pollfd[nfds];

            for (int i = 0; i < send.Length; i++)
            {
                send[i].fd = fds[i].fd;
                send[i].events = (short)fds[i].events;
            }

            int r = sys_poll(send, nfds, timeout);

            for (int i = 0; i < send.Length; i++)
            {
                fds[i].revents = (PollEvents)(send[i].revents);
            }

            return r;
        }

        [DllImport("libc")]
        public extern static int epoll_create1(int size);

        [DllImport("libc")]
        public extern static int epoll_ctl(int epfd, int op, int fd, ref epoll_event __event);

        [DllImport("libc")]
        public extern static int epoll_wait(int epfd, epoll_event* events, int maxevents, int timeout);

        [DllImport("libc")]
        public extern static int pipe2(int* fds, int flags);
        [DllImport("libc")]
        public extern static IntPtr write(int fd, void* buf, IntPtr count);

        [DllImport("libc")]
        public extern static IntPtr read(int fd, void* buf, IntPtr count);

        [DllImport("libdl.so.2")]
        public static extern IntPtr dlopen(string path, int flags);

        [DllImport("libdl.so.2")]
        public static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libdl.so.2")]
        public static extern IntPtr dlerror();

    }
    public struct XrmValue
    {

        /// unsigned int
        public uint size;

        /// XPointer->char*
        public IntPtr addr;
    }


    [Flags]
    public enum PollEvents : short
    {
        POLLIN = 0x0001, // There is data to read
        POLLPRI = 0x0002, // There is urgent data to read
        POLLOUT = 0x0004, // Writing now will not block
        POLLERR = 0x0008, // Error condition
        POLLHUP = 0x0010, // Hung up
        POLLNVAL = 0x0020, // Invalid request; fd not open
                           // XPG4.2 definitions (via _XOPEN_SOURCE)
        POLLRDNORM = 0x0040, // Normal data may be read
        POLLRDBAND = 0x0080, // Priority data may be read
        POLLWRNORM = 0x0100, // Writing now will not block
        POLLWRBAND = 0x0200, // Priority data may be written
    }

    public enum XShapeOperation
    {
        ShapeSet,
        ShapeUnion,
        ShapeIntersect,
        ShapeSubtract,
        ShapeInvert
    }

    public enum XShapeKind
    {
        ShapeBounding,
        ShapeClip,
        //ShapeInput // Not usable without more imports
    }

    public enum XOrdering
    {
        Unsorted,
        YSorted,
        YXSorted,
        YXBanded
    }
}
