using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using static CPF.Linux.XLib;

namespace CPF.Linux
{
    public unsafe class X11Info
    {
        public IntPtr Display { get; }
        public IntPtr DeferredDisplay { get; }
        public int DefaultScreen { get; }
        public IntPtr BlackPixel { get; }
        public IntPtr RootWindow { get; }
        public IntPtr DefaultRootWindow { get; }
        public IntPtr DefaultCursor { get; }
        public X11Atoms Atoms { get; }
        public IntPtr Xim { get; }
        public IntPtr DefaultColormap { get; }

        public int RandrEventBase { get; }
        public int RandrErrorBase { get; }

        public Version RandrVersion { get; }

        public int XInputOpcode { get; }
        public int XInputEventBase { get; }
        public int XInputErrorBase { get; }

        public Version XInputVersion { get; }

        //public IntPtr LastActivityTimestamp { get; set; }
        public XVisualInfo TransparentVisualInfo { get; set; }

        public unsafe X11Info(IntPtr display, IntPtr deferredDisplay)
        {
            Console.WriteLine(setlocale(6, ""));
            ////TODO: Open an actual XIM once we get support for preedit in our textbox
            if (!XSupportsLocale())
            {
                Console.Error.WriteLine("X does not support your locale");
                //return;
            }
            var locale = XSetLocaleModifiers("");
            //if (string.IsNullOrWhiteSpace(locale))
            //{
            //    Console.Error.WriteLine("Could not set X locale modifiers");
            //    //return;
            //}
            //Console.WriteLine(locale);
            //Xim = LinuxPlatform.OpenIM(display);
            Xim = XOpenIM(display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            Console.WriteLine("xim:" + Xim);

            //XGetIMValues(Xim, "queryIMValuesList", out var value, IntPtr.Zero);
            //var list = Marshal.PtrToStructure<XIMValuesList>(value);
            //var str = "";
            //for (int i = 0; i < list.count_values; i++)
            //{
            //    var first = Marshal.ReadIntPtr(list.supported_values + i * IntPtr.Size);
            //    str += Marshal.PtrToStringAnsi(first) + ",";
            //}
            //Console.WriteLine("queryIMValuesList:" + list.count_values + " " + str);


            Display = display;
            DeferredDisplay = deferredDisplay;
            DefaultScreen = XDefaultScreen(display);
            BlackPixel = XBlackPixel(display, DefaultScreen);
            RootWindow = XRootWindow(display, DefaultScreen);
            DefaultCursor = XCreateFontCursor(display, CursorFontShape.XC_top_left_arrow);
            DefaultRootWindow = XDefaultRootWindow(display);
            DefaultColormap = XDefaultColormap(display, DefaultScreen);
            Atoms = new X11Atoms(display, DefaultScreen);
            XMatchVisualInfo(Display, DefaultScreen, 32, 4, out var visual);
            TransparentVisualInfo = visual;
            Console.WriteLine("depth:" + visual.depth);
            try
            {
                if (XRRQueryExtension(display, out int randrEventBase, out var randrErrorBase) != 0)
                {
                    RandrEventBase = randrEventBase;
                    RandrErrorBase = randrErrorBase;
                    if (XRRQueryVersion(display, out var major, out var minor) != 0)
                        RandrVersion = new Version(major, minor);
                }
            }
            catch
            {
                //Ignore, randr is not supported
            }

            try
            {
                if (XQueryExtension(display, "XInputExtension",
                        out var xiopcode, out var xievent, out var xierror))
                {
                    int major = 2, minor = 2;
                    if (XIQueryVersion(display, ref major, ref minor) == Status.Success)
                    {
                        XInputVersion = new Version(major, minor);
                        XInputOpcode = xiopcode;
                        XInputEventBase = xievent;
                        XInputErrorBase = xierror;
                    }
                }
            }
            catch
            {
                //Ignore, XI is not supported
            }
        }
    }
}
