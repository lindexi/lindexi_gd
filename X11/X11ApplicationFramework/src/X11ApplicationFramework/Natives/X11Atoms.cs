using System;
using System.Collections.Generic;
using System.Linq;

namespace X11ApplicationFramework.Natives
{
    // Copy from Avalonia
    public class X11Atoms
    {
        private readonly nint _display;

        public nint AnyPropertyType;

        public nint XA_PRIMARY = 1;

        public nint XA_SECONDARY = 2;

        public nint XA_ARC = 3;

        public nint XA_ATOM = 4;

        public nint XA_BITMAP = 5;

        public nint XA_CARDINAL = 6;

        public nint XA_COLORMAP = 7;

        public nint XA_CURSOR = 8;

        public nint XA_CUT_BUFFER0 = 9;

        public nint XA_CUT_BUFFER1 = 10;

        public nint XA_CUT_BUFFER2 = 11;

        public nint XA_CUT_BUFFER3 = 12;

        public nint XA_CUT_BUFFER4 = 13;

        public nint XA_CUT_BUFFER5 = 14;

        public nint XA_CUT_BUFFER6 = 15;

        public nint XA_CUT_BUFFER7 = 16;

        public nint XA_DRAWABLE = 17;

        public nint XA_FONT = 18;

        public nint XA_INTEGER = 19;

        public nint XA_PIXMAP = 20;

        public nint XA_POINT = 21;

        public nint XA_RECTANGLE = 22;

        public nint XA_RESOURCE_MANAGER = 23;

        public nint XA_RGB_COLOR_MAP = 24;

        public nint XA_RGB_BEST_MAP = 25;

        public nint XA_RGB_BLUE_MAP = 26;

        public nint XA_RGB_DEFAULT_MAP = 27;

        public nint XA_RGB_GRAY_MAP = 28;

        public nint XA_RGB_GREEN_MAP = 29;

        public nint XA_RGB_RED_MAP = 30;

        public nint XA_STRING = 31;

        public nint XA_VISUALID = 32;

        public nint XA_WINDOW = 33;

        public nint XA_WM_COMMAND = 34;

        public nint XA_WM_HINTS = 35;

        public nint XA_WM_CLIENT_MACHINE = 36;

        public nint XA_WM_ICON_NAME = 37;

        public nint XA_WM_ICON_SIZE = 38;

        public nint XA_WM_NAME = 39;

        public nint XA_WM_NORMAL_HINTS = 40;

        public nint XA_WM_SIZE_HINTS = 41;

        public nint XA_WM_ZOOM_HINTS = 42;

        public nint XA_MIN_SPACE = 43;

        public nint XA_NORM_SPACE = 44;

        public nint XA_MAX_SPACE = 45;

        public nint XA_END_SPACE = 46;

        public nint XA_SUPERSCRIPT_X = 47;

        public nint XA_SUPERSCRIPT_Y = 48;

        public nint XA_SUBSCRIPT_X = 49;

        public nint XA_SUBSCRIPT_Y = 50;

        public nint XA_UNDERLINE_POSITION = 51;

        public nint XA_UNDERLINE_THICKNESS = 52;

        public nint XA_STRIKEOUT_ASCENT = 53;

        public nint XA_STRIKEOUT_DESCENT = 54;

        public nint XA_ITALIC_ANGLE = 55;

        public nint XA_X_HEIGHT = 56;

        public nint XA_QUAD_WIDTH = 57;

        public nint XA_WEIGHT = 58;

        public nint XA_POINT_SIZE = 59;

        public nint XA_RESOLUTION = 60;

        public nint XA_COPYRIGHT = 61;

        public nint XA_NOTICE = 62;

        public nint XA_FONT_NAME = 63;

        public nint XA_FAMILY_NAME = 64;

        public nint XA_FULL_NAME = 65;

        public nint XA_CAP_HEIGHT = 66;

        public nint XA_WM_CLASS = 67;

        public nint XA_WM_TRANSIENT_FOR = 68;

        public nint EDID;

        public nint WM_PROTOCOLS;

        public nint WM_DELETE_WINDOW;

        public nint WM_TAKE_FOCUS;

        public nint _NET_SUPPORTED;

        public nint _NET_CLIENT_LIST;

        public nint _NET_NUMBER_OF_DESKTOPS;

        public nint _NET_DESKTOP_GEOMETRY;

        public nint _NET_DESKTOP_VIEWPORT;

        public nint _NET_CURRENT_DESKTOP;

        public nint _NET_DESKTOP_NAMES;

        public nint _NET_ACTIVE_WINDOW;

        public nint _NET_WORKAREA;

        public nint _NET_SUPPORTING_WM_CHECK;

        public nint _NET_VIRTUAL_ROOTS;

        public nint _NET_DESKTOP_LAYOUT;

        public nint _NET_SHOWING_DESKTOP;

        public nint _NET_CLOSE_WINDOW;

        public nint _NET_MOVERESIZE_WINDOW;

        public nint _NET_WM_MOVERESIZE;

        public nint _NET_RESTACK_WINDOW;

        public nint _NET_REQUEST_FRAME_EXTENTS;

        public nint _NET_WM_NAME;

        public nint _NET_WM_VISIBLE_NAME;

        public nint _NET_WM_ICON_NAME;

        public nint _NET_WM_VISIBLE_ICON_NAME;

        public nint _NET_WM_DESKTOP;

        public nint _NET_WM_WINDOW_TYPE;

        public nint _NET_WM_STATE;

        public nint _NET_WM_ALLOWED_ACTIONS;

        public nint _NET_WM_STRUT;

        public nint _NET_WM_STRUT_PARTIAL;

        public nint _NET_WM_ICON_GEOMETRY;

        public nint _NET_WM_ICON;

        public nint _NET_WM_PID;

        public nint _NET_WM_HANDLED_ICONS;

        public nint _NET_WM_USER_TIME;

        public nint _NET_FRAME_EXTENTS;

        public nint _NET_WM_PING;

        public nint _NET_WM_SYNC_REQUEST;

        public nint _NET_WM_SYNC_REQUEST_COUNTER;

        public nint _NET_SYSTEM_TRAY_S;

        public nint _NET_SYSTEM_TRAY_ORIENTATION;

        public nint _NET_SYSTEM_TRAY_OPCODE;

        public nint _NET_WM_STATE_MAXIMIZED_HORZ;

        public nint _NET_WM_STATE_MAXIMIZED_VERT;

        public nint _NET_WM_STATE_FULLSCREEN;

        public nint _XEMBED;

        public nint _XEMBED_INFO;

        public nint _MOTIF_WM_HINTS;

        public nint _NET_WM_STATE_SKIP_TASKBAR;

        public nint _NET_WM_STATE_ABOVE;

        public nint _NET_WM_STATE_MODAL;

        public nint _NET_WM_STATE_HIDDEN;

        public nint _NET_WM_CONTEXT_HELP;

        public nint _NET_WM_WINDOW_OPACITY;

        public nint _NET_WM_WINDOW_TYPE_DESKTOP;

        public nint _NET_WM_WINDOW_TYPE_DOCK;

        public nint _NET_WM_WINDOW_TYPE_TOOLBAR;

        public nint _NET_WM_WINDOW_TYPE_MENU;

        public nint _NET_WM_WINDOW_TYPE_UTILITY;

        public nint _NET_WM_WINDOW_TYPE_SPLASH;

        public nint _NET_WM_WINDOW_TYPE_DIALOG;

        public nint _NET_WM_WINDOW_TYPE_NORMAL;

        public nint CLIPBOARD;

        public nint CLIPBOARD_MANAGER;

        public nint SAVE_TARGETS;

        public nint MULTIPLE;

        public nint PRIMARY;

        public nint OEMTEXT;

        public nint UNICODETEXT;

        public nint TARGETS;

        public nint UTF8_STRING;

        public nint UTF16_STRING;

        public nint ATOM_PAIR;

        public nint MANAGER;

        public nint _KDE_NET_WM_BLUR_BEHIND_REGION;

        public nint INCR;

        public nint _NET_WM_STATE_FOCUSED;

        private readonly Dictionary<string, nint> _namesToAtoms = new Dictionary<string, nint>();

        private readonly Dictionary<nint, string> _atomsToNames = new Dictionary<nint, string>();

        public X11Atoms(nint display)
        {
            _display = display;
            PopulateAtoms(display);
        }

        private void InitAtom(ref nint field, string name, nint value)
        {
            if (value != IntPtr.Zero)
            {
                field = value;
                _namesToAtoms[name] = value;
                _atomsToNames[value] = name;
            }
        }

        public nint GetAtom(string name)
        {
            if (_namesToAtoms.TryGetValue(name, out var value))
            {
                return value;
            }

            nint num = XLib.XInternAtom(_display, name, only_if_exists: false);
            _namesToAtoms[name] = num;
            _atomsToNames[num] = name;
            return num;
        }

        public string? GetAtomName(nint atom)
        {
            if (_atomsToNames.TryGetValue(atom, out string value))
            {
                return value;
            }

            string atomName = XLib.GetAtomName(_display, atom);
            if (atomName == null)
            {
                return null;
            }

            _atomsToNames[atom] = atomName;
            _namesToAtoms[atomName] = atom;
            return atomName;
        }

        private void PopulateAtoms(nint display)
        {
            nint[] array = new nint[148];
            string[] array2 = new string[148]
            {
                "AnyPropertyType", "XA_PRIMARY", "XA_SECONDARY", "XA_ARC", "XA_ATOM", "XA_BITMAP", "XA_CARDINAL",
                "XA_COLORMAP", "XA_CURSOR", "XA_CUT_BUFFER0",
                "XA_CUT_BUFFER1", "XA_CUT_BUFFER2", "XA_CUT_BUFFER3", "XA_CUT_BUFFER4", "XA_CUT_BUFFER5",
                "XA_CUT_BUFFER6", "XA_CUT_BUFFER7", "XA_DRAWABLE", "XA_FONT", "XA_INTEGER",
                "XA_PIXMAP", "XA_POINT", "XA_RECTANGLE", "XA_RESOURCE_MANAGER", "XA_RGB_COLOR_MAP", "XA_RGB_BEST_MAP",
                "XA_RGB_BLUE_MAP", "XA_RGB_DEFAULT_MAP", "XA_RGB_GRAY_MAP", "XA_RGB_GREEN_MAP",
                "XA_RGB_RED_MAP", "XA_STRING", "XA_VISUALID", "XA_WINDOW", "XA_WM_COMMAND", "XA_WM_HINTS",
                "XA_WM_CLIENT_MACHINE", "XA_WM_ICON_NAME", "XA_WM_ICON_SIZE", "XA_WM_NAME",
                "XA_WM_NORMAL_HINTS", "XA_WM_SIZE_HINTS", "XA_WM_ZOOM_HINTS", "XA_MIN_SPACE", "XA_NORM_SPACE",
                "XA_MAX_SPACE", "XA_END_SPACE", "XA_SUPERSCRIPT_X", "XA_SUPERSCRIPT_Y", "XA_SUBSCRIPT_X",
                "XA_SUBSCRIPT_Y", "XA_UNDERLINE_POSITION", "XA_UNDERLINE_THICKNESS", "XA_STRIKEOUT_ASCENT",
                "XA_STRIKEOUT_DESCENT", "XA_ITALIC_ANGLE", "XA_X_HEIGHT", "XA_QUAD_WIDTH", "XA_WEIGHT", "XA_POINT_SIZE",
                "XA_RESOLUTION", "XA_COPYRIGHT", "XA_NOTICE", "XA_FONT_NAME", "XA_FAMILY_NAME", "XA_FULL_NAME",
                "XA_CAP_HEIGHT", "XA_WM_CLASS", "XA_WM_TRANSIENT_FOR", "EDID",
                "WM_PROTOCOLS", "WM_DELETE_WINDOW", "WM_TAKE_FOCUS", "_NET_SUPPORTED", "_NET_CLIENT_LIST",
                "_NET_NUMBER_OF_DESKTOPS", "_NET_DESKTOP_GEOMETRY", "_NET_DESKTOP_VIEWPORT", "_NET_CURRENT_DESKTOP",
                "_NET_DESKTOP_NAMES",
                "_NET_ACTIVE_WINDOW", "_NET_WORKAREA", "_NET_SUPPORTING_WM_CHECK", "_NET_VIRTUAL_ROOTS",
                "_NET_DESKTOP_LAYOUT", "_NET_SHOWING_DESKTOP", "_NET_CLOSE_WINDOW", "_NET_MOVERESIZE_WINDOW",
                "_NET_WM_MOVERESIZE", "_NET_RESTACK_WINDOW",
                "_NET_REQUEST_FRAME_EXTENTS", "_NET_WM_NAME", "_NET_WM_VISIBLE_NAME", "_NET_WM_ICON_NAME",
                "_NET_WM_VISIBLE_ICON_NAME", "_NET_WM_DESKTOP", "_NET_WM_WINDOW_TYPE", "_NET_WM_STATE",
                "_NET_WM_ALLOWED_ACTIONS", "_NET_WM_STRUT",
                "_NET_WM_STRUT_PARTIAL", "_NET_WM_ICON_GEOMETRY", "_NET_WM_ICON", "_NET_WM_PID",
                "_NET_WM_HANDLED_ICONS", "_NET_WM_USER_TIME", "_NET_FRAME_EXTENTS", "_NET_WM_PING",
                "_NET_WM_SYNC_REQUEST", "_NET_WM_SYNC_REQUEST_COUNTER",
                "_NET_SYSTEM_TRAY_S", "_NET_SYSTEM_TRAY_ORIENTATION", "_NET_SYSTEM_TRAY_OPCODE",
                "_NET_WM_STATE_MAXIMIZED_HORZ", "_NET_WM_STATE_MAXIMIZED_VERT", "_NET_WM_STATE_FULLSCREEN", "_XEMBED",
                "_XEMBED_INFO", "_MOTIF_WM_HINTS", "_NET_WM_STATE_SKIP_TASKBAR",
                "_NET_WM_STATE_ABOVE", "_NET_WM_STATE_MODAL", "_NET_WM_STATE_HIDDEN", "_NET_WM_CONTEXT_HELP",
                "_NET_WM_WINDOW_OPACITY", "_NET_WM_WINDOW_TYPE_DESKTOP", "_NET_WM_WINDOW_TYPE_DOCK",
                "_NET_WM_WINDOW_TYPE_TOOLBAR", "_NET_WM_WINDOW_TYPE_MENU", "_NET_WM_WINDOW_TYPE_UTILITY",
                "_NET_WM_WINDOW_TYPE_SPLASH", "_NET_WM_WINDOW_TYPE_DIALOG", "_NET_WM_WINDOW_TYPE_NORMAL", "CLIPBOARD",
                "CLIPBOARD_MANAGER", "SAVE_TARGETS", "MULTIPLE", "PRIMARY", "OEMTEXT", "UNICODETEXT",
                "TARGETS", "UTF8_STRING", "UTF16_STRING", "ATOM_PAIR", "MANAGER", "_KDE_NET_WM_BLUR_BEHIND_REGION",
                "INCR", "_NET_WM_STATE_FOCUSED"
            };
            XLib.XInternAtoms(display, array2, array2.Length, only_if_exists: true, array);
            InitAtom(ref AnyPropertyType, "AnyPropertyType", array[0]);
            InitAtom(ref XA_PRIMARY, "XA_PRIMARY", array[1]);
            InitAtom(ref XA_SECONDARY, "XA_SECONDARY", array[2]);
            InitAtom(ref XA_ARC, "XA_ARC", array[3]);
            InitAtom(ref XA_ATOM, "XA_ATOM", array[4]);
            InitAtom(ref XA_BITMAP, "XA_BITMAP", array[5]);
            InitAtom(ref XA_CARDINAL, "XA_CARDINAL", array[6]);
            InitAtom(ref XA_COLORMAP, "XA_COLORMAP", array[7]);
            InitAtom(ref XA_CURSOR, "XA_CURSOR", array[8]);
            InitAtom(ref XA_CUT_BUFFER0, "XA_CUT_BUFFER0", array[9]);
            InitAtom(ref XA_CUT_BUFFER1, "XA_CUT_BUFFER1", array[10]);
            InitAtom(ref XA_CUT_BUFFER2, "XA_CUT_BUFFER2", array[11]);
            InitAtom(ref XA_CUT_BUFFER3, "XA_CUT_BUFFER3", array[12]);
            InitAtom(ref XA_CUT_BUFFER4, "XA_CUT_BUFFER4", array[13]);
            InitAtom(ref XA_CUT_BUFFER5, "XA_CUT_BUFFER5", array[14]);
            InitAtom(ref XA_CUT_BUFFER6, "XA_CUT_BUFFER6", array[15]);
            InitAtom(ref XA_CUT_BUFFER7, "XA_CUT_BUFFER7", array[16]);
            InitAtom(ref XA_DRAWABLE, "XA_DRAWABLE", array[17]);
            InitAtom(ref XA_FONT, "XA_FONT", array[18]);
            InitAtom(ref XA_INTEGER, "XA_INTEGER", array[19]);
            InitAtom(ref XA_PIXMAP, "XA_PIXMAP", array[20]);
            InitAtom(ref XA_POINT, "XA_POINT", array[21]);
            InitAtom(ref XA_RECTANGLE, "XA_RECTANGLE", array[22]);
            InitAtom(ref XA_RESOURCE_MANAGER, "XA_RESOURCE_MANAGER", array[23]);
            InitAtom(ref XA_RGB_COLOR_MAP, "XA_RGB_COLOR_MAP", array[24]);
            InitAtom(ref XA_RGB_BEST_MAP, "XA_RGB_BEST_MAP", array[25]);
            InitAtom(ref XA_RGB_BLUE_MAP, "XA_RGB_BLUE_MAP", array[26]);
            InitAtom(ref XA_RGB_DEFAULT_MAP, "XA_RGB_DEFAULT_MAP", array[27]);
            InitAtom(ref XA_RGB_GRAY_MAP, "XA_RGB_GRAY_MAP", array[28]);
            InitAtom(ref XA_RGB_GREEN_MAP, "XA_RGB_GREEN_MAP", array[29]);
            InitAtom(ref XA_RGB_RED_MAP, "XA_RGB_RED_MAP", array[30]);
            InitAtom(ref XA_STRING, "XA_STRING", array[31]);
            InitAtom(ref XA_VISUALID, "XA_VISUALID", array[32]);
            InitAtom(ref XA_WINDOW, "XA_WINDOW", array[33]);
            InitAtom(ref XA_WM_COMMAND, "XA_WM_COMMAND", array[34]);
            InitAtom(ref XA_WM_HINTS, "XA_WM_HINTS", array[35]);
            InitAtom(ref XA_WM_CLIENT_MACHINE, "XA_WM_CLIENT_MACHINE", array[36]);
            InitAtom(ref XA_WM_ICON_NAME, "XA_WM_ICON_NAME", array[37]);
            InitAtom(ref XA_WM_ICON_SIZE, "XA_WM_ICON_SIZE", array[38]);
            InitAtom(ref XA_WM_NAME, "XA_WM_NAME", array[39]);
            InitAtom(ref XA_WM_NORMAL_HINTS, "XA_WM_NORMAL_HINTS", array[40]);
            InitAtom(ref XA_WM_SIZE_HINTS, "XA_WM_SIZE_HINTS", array[41]);
            InitAtom(ref XA_WM_ZOOM_HINTS, "XA_WM_ZOOM_HINTS", array[42]);
            InitAtom(ref XA_MIN_SPACE, "XA_MIN_SPACE", array[43]);
            InitAtom(ref XA_NORM_SPACE, "XA_NORM_SPACE", array[44]);
            InitAtom(ref XA_MAX_SPACE, "XA_MAX_SPACE", array[45]);
            InitAtom(ref XA_END_SPACE, "XA_END_SPACE", array[46]);
            InitAtom(ref XA_SUPERSCRIPT_X, "XA_SUPERSCRIPT_X", array[47]);
            InitAtom(ref XA_SUPERSCRIPT_Y, "XA_SUPERSCRIPT_Y", array[48]);
            InitAtom(ref XA_SUBSCRIPT_X, "XA_SUBSCRIPT_X", array[49]);
            InitAtom(ref XA_SUBSCRIPT_Y, "XA_SUBSCRIPT_Y", array[50]);
            InitAtom(ref XA_UNDERLINE_POSITION, "XA_UNDERLINE_POSITION", array[51]);
            InitAtom(ref XA_UNDERLINE_THICKNESS, "XA_UNDERLINE_THICKNESS", array[52]);
            InitAtom(ref XA_STRIKEOUT_ASCENT, "XA_STRIKEOUT_ASCENT", array[53]);
            InitAtom(ref XA_STRIKEOUT_DESCENT, "XA_STRIKEOUT_DESCENT", array[54]);
            InitAtom(ref XA_ITALIC_ANGLE, "XA_ITALIC_ANGLE", array[55]);
            InitAtom(ref XA_X_HEIGHT, "XA_X_HEIGHT", array[56]);
            InitAtom(ref XA_QUAD_WIDTH, "XA_QUAD_WIDTH", array[57]);
            InitAtom(ref XA_WEIGHT, "XA_WEIGHT", array[58]);
            InitAtom(ref XA_POINT_SIZE, "XA_POINT_SIZE", array[59]);
            InitAtom(ref XA_RESOLUTION, "XA_RESOLUTION", array[60]);
            InitAtom(ref XA_COPYRIGHT, "XA_COPYRIGHT", array[61]);
            InitAtom(ref XA_NOTICE, "XA_NOTICE", array[62]);
            InitAtom(ref XA_FONT_NAME, "XA_FONT_NAME", array[63]);
            InitAtom(ref XA_FAMILY_NAME, "XA_FAMILY_NAME", array[64]);
            InitAtom(ref XA_FULL_NAME, "XA_FULL_NAME", array[65]);
            InitAtom(ref XA_CAP_HEIGHT, "XA_CAP_HEIGHT", array[66]);
            InitAtom(ref XA_WM_CLASS, "XA_WM_CLASS", array[67]);
            InitAtom(ref XA_WM_TRANSIENT_FOR, "XA_WM_TRANSIENT_FOR", array[68]);
            InitAtom(ref EDID, "EDID", array[69]);
            InitAtom(ref WM_PROTOCOLS, "WM_PROTOCOLS", array[70]);
            InitAtom(ref WM_DELETE_WINDOW, "WM_DELETE_WINDOW", array[71]);
            InitAtom(ref WM_TAKE_FOCUS, "WM_TAKE_FOCUS", array[72]);
            InitAtom(ref _NET_SUPPORTED, "_NET_SUPPORTED", array[73]);
            InitAtom(ref _NET_CLIENT_LIST, "_NET_CLIENT_LIST", array[74]);
            InitAtom(ref _NET_NUMBER_OF_DESKTOPS, "_NET_NUMBER_OF_DESKTOPS", array[75]);
            InitAtom(ref _NET_DESKTOP_GEOMETRY, "_NET_DESKTOP_GEOMETRY", array[76]);
            InitAtom(ref _NET_DESKTOP_VIEWPORT, "_NET_DESKTOP_VIEWPORT", array[77]);
            InitAtom(ref _NET_CURRENT_DESKTOP, "_NET_CURRENT_DESKTOP", array[78]);
            InitAtom(ref _NET_DESKTOP_NAMES, "_NET_DESKTOP_NAMES", array[79]);
            InitAtom(ref _NET_ACTIVE_WINDOW, "_NET_ACTIVE_WINDOW", array[80]);
            InitAtom(ref _NET_WORKAREA, "_NET_WORKAREA", array[81]);
            InitAtom(ref _NET_SUPPORTING_WM_CHECK, "_NET_SUPPORTING_WM_CHECK", array[82]);
            InitAtom(ref _NET_VIRTUAL_ROOTS, "_NET_VIRTUAL_ROOTS", array[83]);
            InitAtom(ref _NET_DESKTOP_LAYOUT, "_NET_DESKTOP_LAYOUT", array[84]);
            InitAtom(ref _NET_SHOWING_DESKTOP, "_NET_SHOWING_DESKTOP", array[85]);
            InitAtom(ref _NET_CLOSE_WINDOW, "_NET_CLOSE_WINDOW", array[86]);
            InitAtom(ref _NET_MOVERESIZE_WINDOW, "_NET_MOVERESIZE_WINDOW", array[87]);
            InitAtom(ref _NET_WM_MOVERESIZE, "_NET_WM_MOVERESIZE", array[88]);
            InitAtom(ref _NET_RESTACK_WINDOW, "_NET_RESTACK_WINDOW", array[89]);
            InitAtom(ref _NET_REQUEST_FRAME_EXTENTS, "_NET_REQUEST_FRAME_EXTENTS", array[90]);
            InitAtom(ref _NET_WM_NAME, "_NET_WM_NAME", array[91]);
            InitAtom(ref _NET_WM_VISIBLE_NAME, "_NET_WM_VISIBLE_NAME", array[92]);
            InitAtom(ref _NET_WM_ICON_NAME, "_NET_WM_ICON_NAME", array[93]);
            InitAtom(ref _NET_WM_VISIBLE_ICON_NAME, "_NET_WM_VISIBLE_ICON_NAME", array[94]);
            InitAtom(ref _NET_WM_DESKTOP, "_NET_WM_DESKTOP", array[95]);
            InitAtom(ref _NET_WM_WINDOW_TYPE, "_NET_WM_WINDOW_TYPE", array[96]);
            InitAtom(ref _NET_WM_STATE, "_NET_WM_STATE", array[97]);
            InitAtom(ref _NET_WM_ALLOWED_ACTIONS, "_NET_WM_ALLOWED_ACTIONS", array[98]);
            InitAtom(ref _NET_WM_STRUT, "_NET_WM_STRUT", array[99]);
            InitAtom(ref _NET_WM_STRUT_PARTIAL, "_NET_WM_STRUT_PARTIAL", array[100]);
            InitAtom(ref _NET_WM_ICON_GEOMETRY, "_NET_WM_ICON_GEOMETRY", array[101]);
            InitAtom(ref _NET_WM_ICON, "_NET_WM_ICON", array[102]);
            InitAtom(ref _NET_WM_PID, "_NET_WM_PID", array[103]);
            InitAtom(ref _NET_WM_HANDLED_ICONS, "_NET_WM_HANDLED_ICONS", array[104]);
            InitAtom(ref _NET_WM_USER_TIME, "_NET_WM_USER_TIME", array[105]);
            InitAtom(ref _NET_FRAME_EXTENTS, "_NET_FRAME_EXTENTS", array[106]);
            InitAtom(ref _NET_WM_PING, "_NET_WM_PING", array[107]);
            InitAtom(ref _NET_WM_SYNC_REQUEST, "_NET_WM_SYNC_REQUEST", array[108]);
            InitAtom(ref _NET_WM_SYNC_REQUEST_COUNTER, "_NET_WM_SYNC_REQUEST_COUNTER", array[109]);
            InitAtom(ref _NET_SYSTEM_TRAY_S, "_NET_SYSTEM_TRAY_S", array[110]);
            InitAtom(ref _NET_SYSTEM_TRAY_ORIENTATION, "_NET_SYSTEM_TRAY_ORIENTATION", array[111]);
            InitAtom(ref _NET_SYSTEM_TRAY_OPCODE, "_NET_SYSTEM_TRAY_OPCODE", array[112]);
            InitAtom(ref _NET_WM_STATE_MAXIMIZED_HORZ, "_NET_WM_STATE_MAXIMIZED_HORZ", array[113]);
            InitAtom(ref _NET_WM_STATE_MAXIMIZED_VERT, "_NET_WM_STATE_MAXIMIZED_VERT", array[114]);
            InitAtom(ref _NET_WM_STATE_FULLSCREEN, "_NET_WM_STATE_FULLSCREEN", array[115]);
            InitAtom(ref _XEMBED, "_XEMBED", array[116]);
            InitAtom(ref _XEMBED_INFO, "_XEMBED_INFO", array[117]);
            InitAtom(ref _MOTIF_WM_HINTS, "_MOTIF_WM_HINTS", array[118]);
            InitAtom(ref _NET_WM_STATE_SKIP_TASKBAR, "_NET_WM_STATE_SKIP_TASKBAR", array[119]);
            InitAtom(ref _NET_WM_STATE_ABOVE, "_NET_WM_STATE_ABOVE", array[120]);
            InitAtom(ref _NET_WM_STATE_MODAL, "_NET_WM_STATE_MODAL", array[121]);
            InitAtom(ref _NET_WM_STATE_HIDDEN, "_NET_WM_STATE_HIDDEN", array[122]);
            InitAtom(ref _NET_WM_CONTEXT_HELP, "_NET_WM_CONTEXT_HELP", array[123]);
            InitAtom(ref _NET_WM_WINDOW_OPACITY, "_NET_WM_WINDOW_OPACITY", array[124]);
            InitAtom(ref _NET_WM_WINDOW_TYPE_DESKTOP, "_NET_WM_WINDOW_TYPE_DESKTOP", array[125]);
            InitAtom(ref _NET_WM_WINDOW_TYPE_DOCK, "_NET_WM_WINDOW_TYPE_DOCK", array[126]);
            InitAtom(ref _NET_WM_WINDOW_TYPE_TOOLBAR, "_NET_WM_WINDOW_TYPE_TOOLBAR", array[127]);
            InitAtom(ref _NET_WM_WINDOW_TYPE_MENU, "_NET_WM_WINDOW_TYPE_MENU", array[128]);
            InitAtom(ref _NET_WM_WINDOW_TYPE_UTILITY, "_NET_WM_WINDOW_TYPE_UTILITY", array[129]);
            InitAtom(ref _NET_WM_WINDOW_TYPE_SPLASH, "_NET_WM_WINDOW_TYPE_SPLASH", array[130]);
            InitAtom(ref _NET_WM_WINDOW_TYPE_DIALOG, "_NET_WM_WINDOW_TYPE_DIALOG", array[131]);
            InitAtom(ref _NET_WM_WINDOW_TYPE_NORMAL, "_NET_WM_WINDOW_TYPE_NORMAL", array[132]);
            InitAtom(ref CLIPBOARD, "CLIPBOARD", array[133]);
            InitAtom(ref CLIPBOARD_MANAGER, "CLIPBOARD_MANAGER", array[134]);
            InitAtom(ref SAVE_TARGETS, "SAVE_TARGETS", array[135]);
            InitAtom(ref MULTIPLE, "MULTIPLE", array[136]);
            InitAtom(ref PRIMARY, "PRIMARY", array[137]);
            InitAtom(ref OEMTEXT, "OEMTEXT", array[138]);
            InitAtom(ref UNICODETEXT, "UNICODETEXT", array[139]);
            InitAtom(ref TARGETS, "TARGETS", array[140]);
            InitAtom(ref UTF8_STRING, "UTF8_STRING", array[141]);
            InitAtom(ref UTF16_STRING, "UTF16_STRING", array[142]);
            InitAtom(ref ATOM_PAIR, "ATOM_PAIR", array[143]);
            InitAtom(ref MANAGER, "MANAGER", array[144]);
            InitAtom(ref _KDE_NET_WM_BLUR_BEHIND_REGION, "_KDE_NET_WM_BLUR_BEHIND_REGION", array[145]);
            InitAtom(ref INCR, "INCR", array[146]);
            InitAtom(ref _NET_WM_STATE_FOCUSED, "_NET_WM_STATE_FOCUSED", array[147]);
        }
    }
}