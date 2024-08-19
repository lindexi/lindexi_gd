using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using CPF.Linux;

using ShmSeg = System.UInt64;

namespace CPF.Linux;

internal unsafe class XShm
{
    [DllImport("libXext.so.6", SetLastError = true)]
    public static extern int XShmQueryExtension(IntPtr display);

    /*
    Status XShmQueryVersion (display, major, minor, pixmaps)
      Display *display;
      int *major, *minor;
      Bool *pixmaps
    */
    [DllImport("libXext.so.6", SetLastError = true)]
    public static extern int XShmQueryVersion(IntPtr display, out int major, out int minor, out bool pixmaps);

    [DllImport("libXext.so.6", SetLastError = true)]
    public static extern int XShmPutImage(IntPtr display, IntPtr drawable, IntPtr gc, XImage* image, int src_x, int src_y,
        int dst_x, int dst_y, uint src_width, uint src_height, bool send_event);

    // XShmAttach(display, &shminfo);
    [DllImport("libXext.so.6", SetLastError = true)]
    public static extern int XShmAttach(IntPtr display, XShmSegmentInfo* shminfo);

    /*
    XImage *XShmCreateImage (display, visual, depth, format, data,
                       shminfo, width, height)
      Display *display;
      Visual *visual;
      unsigned int depth, width, height;
      int format;
      char *data;
      XShmSegmentInfo *shminfo;
    */
    [DllImport("libXext.so.6", SetLastError = true)]
    public static extern IntPtr XShmCreateImage(IntPtr display, IntPtr visual, uint depth, int format, IntPtr data,
        XShmSegmentInfo* shminfo, uint width, uint height);
}

[StructLayout(LayoutKind.Sequential)]
unsafe struct XShmSegmentInfo
{
    public ShmSeg shmseg; /* resource id */
    public int shmid; /* kernel id */
    public char* shmaddr; /* address in client */
    public bool readOnly; /* how the server should attach it */

    public override string ToString()
    {
        return
            $"XShmSegmentInfo {{ shmseg = {shmseg}, shmid = {shmid}, shmaddr = {new IntPtr(shmaddr).ToString("X")}, readOnly = {readOnly} }}";
    }
}