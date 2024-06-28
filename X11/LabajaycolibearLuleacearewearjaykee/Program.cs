using System.Runtime.InteropServices;

using static CPF.Linux.XLib;

var display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(display);

var rootWindow = XDefaultRootWindow(display);
var xRootWindow = XRootWindow(display, screen);

//var screenHandler = XScreenOfDisplay(display, screen);
//Console.WriteLine($"screenHandler={screenHandler}");

unsafe
{
    var pDisplay = (Display*)display;
    //Console.WriteLine($"nscreens={pDisplay->nscreens} Screens={pDisplay->Screens} screenHandler={screenHandler}");

    var firstScreen = pDisplay->Screens;

    Screen* pScreen = (Screen*) firstScreen;

    //Console.WriteLine($"XDisplay={pScreen->_XDisplay} display={display}");

    var rootWindowFromPScreen = pScreen->RootWindow;

    Console.WriteLine($"{rootWindowFromPScreen} xRootWindow={xRootWindow} rootWindow={rootWindow}");

    Console.WriteLine($"w={pScreen->Width} h={pScreen->Height}");
}

const string libX11 = "libX11.so.6";

[DllImport(libX11)]
static extern IntPtr XScreenOfDisplay(IntPtr display, int screen_number);

/*
typedef struct
   #ifdef XLIB_ILLEGAL_ACCESS
   _XDisplay
   #endif
   {
    	XExtData *ext_data;	/* hook for extension to hang data * /
    	struct _XPrivate *private1;
    	int fd;			/* Network socket. * /
    	int private2;
    	int proto_major_version;/* major version of server's X protocol * /
    	int proto_minor_version;/* minor version of servers X protocol * /
    	char *vendor;		/* vendor of the server hardware * /
            XID private3;
    	XID private4;
    	XID private5;
    	int private6;
    	XID (*resource_alloc)(	/* allocator function * /
   		struct _XDisplay*
   	);
    	int byte_order;		/* screen byte order, LSBFirst, MSBFirst * /
    	int bitmap_unit;	/* padding and data requirements * /
    	int bitmap_pad;		/* padding requirements on bitmaps * /
    	int bitmap_bit_order;	/* LeastSignificant or MostSignificant * /
    	int nformats;		/* number of pixmap formats in list * /
    	ScreenFormat *pixmap_format;	/* pixmap format list * /
    	int private8;
    	int release;		/* release of the server * /
    	struct _XPrivate *private9, *private10;
    	int qlen;		/* Length of input event queue * /
    	unsigned long last_request_read; /* seq number of last event read * /
    	unsigned long request;	/* sequence number of last request. * /
   	XPointer private11;
   	XPointer private12;
   	XPointer private13;
   	XPointer private14;
   	unsigned max_request_size; /* maximum number 32 bit words in request* /
   	struct _XrmHashBucketRec *db;
   	int (*private15)(
   		struct _XDisplay*
   		);
   	char *display_name;	/* "host:display" string used on this connect* /
   	int default_screen;	/* default screen for operations * /
   	int nscreens;		/* number of screens on this server* /
   	Screen *screens;	/* pointer to list of screens * /
   	unsigned long motion_buffer;	/* size of motion buffer * /
   	unsigned long private16;
   	int min_keycode;	/* minimum defined keycode * /
   	int max_keycode;	/* maximum defined keycode * /
   	XPointer private17;
   	XPointer private18;
   	int private19;
   	char *xdefaults;	/* contents of defaults from server * /
   	/* there is more to this structure, but it is private to Xlib * /
   }
   #ifdef XLIB_ILLEGAL_ACCESS
   Display,
   #endif
   *_XPrivDisplay;
 */

/*
typedef struct {
   	XExtData *ext_data;	/* hook for extension to hang data * /
   	struct _XDisplay *display;/* back pointer to display structure * /
   	Window root;		/* Root window id. * /
   	int width, height;	/* width and height of screen * /
   	int mwidth, mheight;	/* width and height of  in millimeters * /
   	int ndepths;		/* number of depths possible * /
   	Depth *depths;		/* list of allowable depths on the screen * /
   	int root_depth;		/* bits per pixel * /
   	Visual *root_visual;	/* root visual * /
   	GC default_gc;		/* GC for the root root visual * /
   	Colormap cmap;		/* default color map * /
   	unsigned long white_pixel;
   	unsigned long black_pixel;	/* White and Black pixel values * /
   	int max_maps, min_maps;	/* max and min color maps * /
   	int backing_store;	/* Never, WhenMapped, Always * /
   	Bool save_unders;
   	long root_input_mask;	/* initial root input mask * /
   } Screen;
 */



Console.WriteLine("Hello, World!");



[StructLayout(LayoutKind.Explicit)]
struct Screen
{
    /*
       XExtData *ext_data;	/* hook for extension to hang data * /
       struct _XDisplay *display;/* back pointer to display structure * /
       Window root;		/* Root window id. * /
     */
    [FieldOffset(8)]
    public IntPtr _XDisplay;

    [FieldOffset(8 + 8)]
    public int RootWindow;

    [FieldOffset(8 + 8 + 8)]
    public int Width;
    [FieldOffset(8 + 8 + 8 + 4)]
    public int Height;
}

[StructLayout(LayoutKind.Explicit)]
struct Display
{
    [FieldOffset(228)]
    public int nscreens;

    [FieldOffset(232)]
    public IntPtr Screens;
}