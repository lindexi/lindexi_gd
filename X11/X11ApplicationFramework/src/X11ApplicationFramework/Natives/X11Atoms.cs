using System;
using System.Linq;

namespace X11ApplicationFramework.Natives
{
    public class X11Atoms
    {

        // Our atoms
        public const IntPtr AnyPropertyType = (IntPtr) 0;
        public const IntPtr XA_PRIMARY = (IntPtr) 1;
        public const IntPtr XA_SECONDARY = (IntPtr) 2;
        public const IntPtr XA_ARC = (IntPtr) 3;
        public const IntPtr XA_ATOM = (IntPtr) 4;
        public const IntPtr XA_BITMAP = (IntPtr) 5;
        public const IntPtr XA_CARDINAL = (IntPtr) 6;
        public const IntPtr XA_COLORMAP = (IntPtr) 7;
        public const IntPtr XA_CURSOR = (IntPtr) 8;
        public const IntPtr XA_CUT_BUFFER0 = (IntPtr) 9;
        public const IntPtr XA_CUT_BUFFER1 = (IntPtr) 10;
        public const IntPtr XA_CUT_BUFFER2 = (IntPtr) 11;
        public const IntPtr XA_CUT_BUFFER3 = (IntPtr) 12;
        public const IntPtr XA_CUT_BUFFER4 = (IntPtr) 13;
        public const IntPtr XA_CUT_BUFFER5 = (IntPtr) 14;
        public const IntPtr XA_CUT_BUFFER6 = (IntPtr) 15;
        public const IntPtr XA_CUT_BUFFER7 = (IntPtr) 16;
        public const IntPtr XA_DRAWABLE = (IntPtr) 17;
        public const IntPtr XA_FONT = (IntPtr) 18;
        public const IntPtr XA_INTEGER = (IntPtr) 19;
        public const IntPtr XA_PIXMAP = (IntPtr) 20;
        public const IntPtr XA_POINT = (IntPtr) 21;
        public const IntPtr XA_RECTANGLE = (IntPtr) 22;
        public const IntPtr XA_RESOURCE_MANAGER = (IntPtr) 23;
        public const IntPtr XA_RGB_COLOR_MAP = (IntPtr) 24;
        public const IntPtr XA_RGB_BEST_MAP = (IntPtr) 25;
        public const IntPtr XA_RGB_BLUE_MAP = (IntPtr) 26;
        public const IntPtr XA_RGB_DEFAULT_MAP = (IntPtr) 27;
        public const IntPtr XA_RGB_GRAY_MAP = (IntPtr) 28;
        public const IntPtr XA_RGB_GREEN_MAP = (IntPtr) 29;
        public const IntPtr XA_RGB_RED_MAP = (IntPtr) 30;
        public const IntPtr XA_STRING = (IntPtr) 31;
        public const IntPtr XA_VISUALID = (IntPtr) 32;
        public const IntPtr XA_WINDOW = (IntPtr) 33;
        public const IntPtr XA_WM_COMMAND = (IntPtr) 34;
        public const IntPtr XA_WM_HINTS = (IntPtr) 35;
        public const IntPtr XA_WM_CLIENT_MACHINE = (IntPtr) 36;
        public const IntPtr XA_WM_ICON_NAME = (IntPtr) 37;
        public const IntPtr XA_WM_ICON_SIZE = (IntPtr) 38;
        public const IntPtr XA_WM_NAME = (IntPtr) 39;
        public const IntPtr XA_WM_NORMAL_HINTS = (IntPtr) 40;
        public const IntPtr XA_WM_SIZE_HINTS = (IntPtr) 41;
        public const IntPtr XA_WM_ZOOM_HINTS = (IntPtr) 42;
        public const IntPtr XA_MIN_SPACE = (IntPtr) 43;
        public const IntPtr XA_NORM_SPACE = (IntPtr) 44;
        public const IntPtr XA_MAX_SPACE = (IntPtr) 45;
        public const IntPtr XA_END_SPACE = (IntPtr) 46;
        public const IntPtr XA_SUPERSCRIPT_X = (IntPtr) 47;
        public const IntPtr XA_SUPERSCRIPT_Y = (IntPtr) 48;
        public const IntPtr XA_SUBSCRIPT_X = (IntPtr) 49;
        public const IntPtr XA_SUBSCRIPT_Y = (IntPtr) 50;
        public const IntPtr XA_UNDERLINE_POSITION = (IntPtr) 51;
        public const IntPtr XA_UNDERLINE_THICKNESS = (IntPtr) 52;
        public const IntPtr XA_STRIKEOUT_ASCENT = (IntPtr) 53;
        public const IntPtr XA_STRIKEOUT_DESCENT = (IntPtr) 54;
        public const IntPtr XA_ITALIC_ANGLE = (IntPtr) 55;
        public const IntPtr XA_X_HEIGHT = (IntPtr) 56;
        public const IntPtr XA_QUAD_WIDTH = (IntPtr) 57;
        public const IntPtr XA_WEIGHT = (IntPtr) 58;
        public const IntPtr XA_POINT_SIZE = (IntPtr) 59;
        public const IntPtr XA_RESOLUTION = (IntPtr) 60;
        public const IntPtr XA_COPYRIGHT = (IntPtr) 61;
        public const IntPtr XA_NOTICE = (IntPtr) 62;
        public const IntPtr XA_FONT_NAME = (IntPtr) 63;
        public const IntPtr XA_FAMILY_NAME = (IntPtr) 64;
        public const IntPtr XA_FULL_NAME = (IntPtr) 65;
        public const IntPtr XA_CAP_HEIGHT = (IntPtr) 66;
        public const IntPtr XA_WM_CLASS = (IntPtr) 67;
        public const IntPtr XA_WM_TRANSIENT_FOR = (IntPtr) 68;

        public readonly IntPtr WM_PROTOCOLS;
        public readonly IntPtr WM_DELETE_WINDOW;
        public readonly IntPtr WM_TAKE_FOCUS;
        public readonly IntPtr _NET_SUPPORTED;
        public readonly IntPtr _NET_CLIENT_LIST;
        public readonly IntPtr _NET_NUMBER_OF_DESKTOPS;
        public readonly IntPtr _NET_DESKTOP_GEOMETRY;
        public readonly IntPtr _NET_DESKTOP_VIEWPORT;
        public readonly IntPtr _NET_CURRENT_DESKTOP;
        public readonly IntPtr _NET_DESKTOP_NAMES;
        public readonly IntPtr _NET_ACTIVE_WINDOW;
        public readonly IntPtr _NET_WORKAREA;
        public readonly IntPtr _NET_SUPPORTING_WM_CHECK;
        public readonly IntPtr _NET_VIRTUAL_ROOTS;
        public readonly IntPtr _NET_DESKTOP_LAYOUT;
        public readonly IntPtr _NET_SHOWING_DESKTOP;
        public readonly IntPtr _NET_CLOSE_WINDOW;
        public readonly IntPtr _NET_MOVERESIZE_WINDOW;
        public readonly IntPtr _NET_WM_MOVERESIZE;
        public readonly IntPtr _NET_RESTACK_WINDOW;
        public readonly IntPtr _NET_REQUEST_FRAME_EXTENTS;
        public readonly IntPtr _NET_WM_NAME;
        public readonly IntPtr _NET_WM_VISIBLE_NAME;
        public readonly IntPtr _NET_WM_ICON_NAME;
        public readonly IntPtr _NET_WM_VISIBLE_ICON_NAME;
        public readonly IntPtr _NET_WM_DESKTOP;
        public readonly IntPtr _NET_WM_WINDOW_TYPE;
        public readonly IntPtr _NET_WM_STATE;
        public readonly IntPtr _NET_WM_ALLOWED_ACTIONS;
        public readonly IntPtr _NET_WM_STRUT;
        public readonly IntPtr _NET_WM_STRUT_PARTIAL;
        public readonly IntPtr _NET_WM_ICON_GEOMETRY;
        public readonly IntPtr _NET_WM_ICON;
        public readonly IntPtr _NET_WM_PID;
        public readonly IntPtr _NET_WM_HANDLED_ICONS;
        public readonly IntPtr _NET_WM_USER_TIME;
        public readonly IntPtr _NET_FRAME_EXTENTS;
        public readonly IntPtr _NET_WM_PING;
        public readonly IntPtr _NET_WM_SYNC_REQUEST;
        public readonly IntPtr _NET_SYSTEM_TRAY_S;
        public readonly IntPtr _NET_SYSTEM_TRAY_ORIENTATION;
        public readonly IntPtr _NET_SYSTEM_TRAY_OPCODE;
        public readonly IntPtr _NET_WM_STATE_MAXIMIZED_HORZ;
        public readonly IntPtr _NET_WM_STATE_MAXIMIZED_VERT;
        public readonly IntPtr _XEMBED;
        public readonly IntPtr _XEMBED_INFO;
        public readonly IntPtr _MOTIF_WM_HINTS;
        public readonly IntPtr _NET_WM_STATE_SKIP_TASKBAR;
        public readonly IntPtr _NET_WM_STATE_ABOVE;
        public readonly IntPtr _NET_WM_STATE_MODAL;
        public readonly IntPtr _NET_WM_STATE_HIDDEN;
        public readonly IntPtr _NET_WM_STATE_FOCUSED;
        public readonly IntPtr _NET_WM_CONTEXT_HELP;
        public readonly IntPtr _NET_WM_WINDOW_OPACITY;
        public readonly IntPtr _NET_WM_WINDOW_TYPE_DESKTOP;
        public readonly IntPtr _NET_WM_WINDOW_TYPE_DOCK;
        public readonly IntPtr _NET_WM_WINDOW_TYPE_TOOLBAR;
        public readonly IntPtr _NET_WM_WINDOW_TYPE_MENU;
        public readonly IntPtr _NET_WM_WINDOW_TYPE_UTILITY;
        public readonly IntPtr _NET_WM_WINDOW_TYPE_SPLASH;
        public readonly IntPtr _NET_WM_WINDOW_TYPE_DIALOG;
        public readonly IntPtr _NET_WM_WINDOW_TYPE_NORMAL;
        public readonly IntPtr _NET_WM_STATE_FULLSCREEN;
        public readonly IntPtr CLIPBOARD;
        public readonly IntPtr CLIPBOARD_MANAGER;
        public readonly IntPtr SAVE_TARGETS;
        public readonly IntPtr MULTIPLE;
        public readonly IntPtr PRIMARY;
        public readonly IntPtr OEMTEXT;
        public readonly IntPtr UNICODETEXT;
        public readonly IntPtr TARGETS;
        public readonly IntPtr UTF8_STRING;
        public readonly IntPtr UTF16_STRING;
        public readonly IntPtr ATOM_PAIR;
        public readonly IntPtr STRING;
        public readonly IntPtr TEXT;
        public readonly IntPtr COMPOUND_TEXT;

        public readonly IntPtr XdndActionCopy;
        public readonly IntPtr XdndActionMove;
        public readonly IntPtr XdndActionLink;
        //static IntPtr XdndActionPrivate;
        public readonly IntPtr XdndActionList;
        public readonly IntPtr XdndAware;
        public readonly IntPtr XdndEnter;
        public readonly IntPtr XdndLeave;
        public readonly IntPtr XdndPosition;
        public readonly IntPtr XdndStatus;
        public readonly IntPtr XdndDrop;
        public readonly IntPtr XdndSelection;
        public readonly IntPtr XdndFinished;
        public readonly IntPtr XdndTypeList;

        public readonly IntPtr EDID;

        //自定义消息
        public static IntPtr Invoke = (IntPtr) 1500;
        public static IntPtr BeginInvoke = (IntPtr) 1501;
        public X11Atoms(IntPtr display, int screen)
        {

            // make sure this array stays in sync with the statements below

            var fields = typeof(X11Atoms).GetFields()
<<<<<<< HEAD
                .Where(f => f.FieldType == typeof(IntPtr) && (IntPtr)f.GetValue(this)! == IntPtr.Zero).ToArray();
=======
                .Where(f =>
                {
                    if (f.FieldType != typeof(IntPtr))
                    {
                        return false;
                    }

                    if (f.IsLiteral)
                    {
                        // 如果是常量，也不能设置
                        return false;
                    }

                    try
                    {
                        var value = f.GetValue(this);
                        if (value is IntPtr p)
                        {
                            return p == 0;
                        }
                        else if (value is int n)
                        {
                            return n == 0;
                        }

                        return false;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Field Fail. Name={f.Name} {e}");
                        throw;
                    }
                }).ToArray();
>>>>>>> 5b15653c089f8362f8cfea5ca7443c18800e97cb
            var atomNames = fields.Select(f => f.Name).ToArray();

            IntPtr[] atoms = new IntPtr[atomNames.Length];


            XLib.XInternAtoms(display, atomNames, atomNames.Length, true, atoms);

            for (var c = 0; c < fields.Length; c++)
                fields[c].SetValue(this, atoms[c]);

            _NET_SYSTEM_TRAY_S = XLib.XInternAtom(display, "_NET_SYSTEM_TRAY_S" + screen, false);
        }
    }
}
