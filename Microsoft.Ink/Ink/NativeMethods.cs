// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.NativeMethods
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  internal static class NativeMethods
  {
    public static IntPtr NullIntPtr = (IntPtr) 0;

    [StructLayout(LayoutKind.Sequential)]
    public class PICTDESCicon
    {
      internal int cbSizeOfStruct = 20;
      internal int picType = 3;
      internal IntPtr hicon = NativeMethods.NullIntPtr;
      internal int unused1;
      internal int unused2;

      public PICTDESCicon(Icon icon) => this.hicon = icon != null ? icon.Handle : throw new ArgumentNullException(nameof (icon), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
    }

    [ComVisible(false)]
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class CHARFORMATA
    {
      public int cbSize;
      public int dwMask;
      public int dwEffects;
      public int yHeight;
      public int yOffset;
      public int crTextColor;
      public byte bCharSet;
      public byte bPitchAndFamily;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
      public char[] szFaceName = new char[32];

      public CHARFORMATA()
      {
        this.cbSize = Marshal.SizeOf((object) this);
        this.dwMask = 0;
        this.dwEffects = 0;
        this.yHeight = 0;
        this.yOffset = 0;
        this.crTextColor = 0;
        this.bCharSet = (byte) 0;
        this.bPitchAndFamily = (byte) 0;
      }
    }

    [ComVisible(false)]
    [StructLayout(LayoutKind.Sequential)]
    public class NMHDR
    {
      public IntPtr hwndFrom = IntPtr.Zero;
      public int idFrom;
      public int code;
    }

    [ComVisible(false)]
    [StructLayout(LayoutKind.Sequential)]
    public class IEC_RECOGNITIONRESULTINFO
    {
      public NativeMethods.NMHDR nmhdr;
      public IInkRecognitionResult RecognitionResult;
    }

    [ComVisible(false)]
    [StructLayout(LayoutKind.Sequential)]
    public class IEC_STROKEINFO
    {
      public NativeMethods.NMHDR nmhdr;
      public IInkCursor Cursor;
      public IInkStrokeDisp Stroke;
    }

    [ComVisible(false)]
    [StructLayout(LayoutKind.Sequential)]
    public class IEC_GESTUREINFO
    {
      public NativeMethods.NMHDR nmhdr;
      public IInkCursor Cursor;
      public InkStrokes Strokes;
      [MarshalAs(UnmanagedType.Struct)]
      public object Gestures;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class BITMAPINFO
    {
      public int bmiHeader_biSize;
      public int bmiHeader_biWidth;
      public int bmiHeader_biHeight;
      public short bmiHeader_biPlanes;
      public short bmiHeader_biBitCount;
      public int bmiHeader_biCompression;
      public int bmiHeader_biSizeImage;
      public int bmiHeader_biXPelsPerMeter;
      public int bmiHeader_biYPelsPerMeter;
      public int bmiHeader_biClrUsed;
      public int bmiHeader_biClrImportant;
      public byte bmiColors_rgbBlue;
      public byte bmiColors_rgbGreen;
      public byte bmiColors_rgbRed;
      public byte bmiColors_rgbReserved;

      public BITMAPINFO()
      {
        this.bmiHeader_biSize = 0;
        this.bmiHeader_biWidth = 0;
        this.bmiHeader_biHeight = 0;
        this.bmiHeader_biPlanes = (short) 0;
        this.bmiHeader_biBitCount = (short) 0;
        this.bmiHeader_biCompression = 0;
        this.bmiHeader_biSizeImage = 0;
        this.bmiHeader_biXPelsPerMeter = 0;
        this.bmiHeader_biYPelsPerMeter = 0;
        this.bmiHeader_biClrUsed = 0;
        this.bmiHeader_biClrImportant = 0;
        this.bmiColors_rgbBlue = (byte) 0;
        this.bmiColors_rgbGreen = (byte) 0;
        this.bmiColors_rgbRed = (byte) 0;
        this.bmiColors_rgbReserved = (byte) 0;
      }
    }

    [ComVisible(false)]
    public class Ole
    {
      public const int PICTYPE_UNINITIALIZED = -1;
      public const int PICTYPE_NONE = 0;
      public const int PICTYPE_BITMAP = 1;
      public const int PICTYPE_METAFILE = 2;
      public const int PICTYPE_ICON = 3;
      public const int PICTYPE_ENHMETAFILE = 4;
      public const int STATFLAG_DEFAULT = 0;
      public const int STATFLAG_NONAME = 1;
    }
  }
}
