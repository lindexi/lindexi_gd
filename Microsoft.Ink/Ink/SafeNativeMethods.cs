// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.SafeNativeMethods
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  internal static class SafeNativeMethods
  {
    [DllImport("oleaut32.dll", EntryPoint = "OleCreatePictureIndirect", PreserveSig = false)]
    public static extern SafeNativeMethods.IPictureDisp OleCreateIPictureDispIndirect(
      [MarshalAs(UnmanagedType.AsAny)] object pictdesc,
      ref Guid iid,
      bool fOwn);

    [DllImport("InkDiv.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr CreateInkDivider();

    [DllImport("InkDiv.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    public static extern void DeleteInkDivider(IntPtr hDivider);

    [DllImport("InkDiv.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    public static extern void CallDivide(
      IntPtr hDivider,
      out int wordSize,
      out int lineSize,
      out int paragraphSize,
      out int drawingSize,
      out int wordCount,
      out int lineCount,
      out int paragraphCount);

    [DllImport("InkDiv.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    public static extern void CallDivideResults(
      IntPtr hDivider,
      int[] aWordStrokeIds,
      int[] aLineStrokeIds,
      int[] aParagraphStrokeIds,
      int[] aDrawingStrokeIds,
      [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] astrWords,
      [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] astrLines,
      [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] astrParagraphs,
      int[] aWordRotationCenterX,
      int[] aWordRotationCenterY,
      float[] aWordAngles,
      int[] aLineRotationCenterX,
      int[] aLineRotationCenterY,
      float[] aLineAngles);

    [DllImport("InkDiv.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    public static extern void RecognizerContextSet(IntPtr hDivider);

    [DllImport("InkDiv.dll", CharSet = CharSet.Unicode)]
    public static extern void SetLineHeight(IntPtr hDivider, int LineHeight);

    [DllImport("InkDiv.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    public static extern void AddOneStroke(
      IntPtr hDivider,
      int strokeId,
      int cPoints,
      Point[] aPoints);

    [DllImport("InkDiv.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    public static extern void RemoveStrokes(IntPtr hDivider, int cStrokes, int[] aStrokeIds);

    [DllImport("InkDiv.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    public static extern void SetLineRecoCallback(
      IntPtr hDivider,
      SafeNativeMethods.GetLineRecoDef callback);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
    public static extern int SelectClipRgn(HandleRef hDC, HandleRef hRgn);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
    public static extern int GetDIBits(
      IntPtr hdc,
      IntPtr hBmp,
      int iStartScan,
      int cScanLines,
      IntPtr lpBits,
      NativeMethods.BITMAPINFO bmi,
      int fuColorUse);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
    public static extern int SetDIBits(
      IntPtr hDC,
      IntPtr hBmp,
      int iStartScan,
      int cScanLines,
      IntPtr lpBits,
      NativeMethods.BITMAPINFO bmi,
      int fuColorUse);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
    public static extern bool DeleteObject(HandleRef hObject);

    [DllImport("inkobj.dll", PreserveSig = false)]
    public static extern void InvokeIDispatch(
      IntPtr pDispatch,
      int dispid,
      IntPtr pDisp,
      IntPtr pVarResult);

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("7BF80981-BF32-101A-8BBB-00AA00300CAB")]
    [ComImport]
    public interface IPictureDisp
    {
      IntPtr GetHandle();

      IntPtr GetHPal();

      void SetHPal(IntPtr newValue);

      [return: MarshalAs(UnmanagedType.I2)]
      short GetPictureType();

      int GetWidth();

      int GetHeight();

      void Render(
        IntPtr hdc,
        int x,
        int y,
        int cx,
        int cy,
        int xSrc,
        int ySrc,
        int cxSrc,
        int cySrc);
    }

    public delegate void GetLineRecoDef(
      int cStrokes,
      [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I4)] int[] aStrokeIds,
      float degrees,
      Point point,
      [MarshalAs(UnmanagedType.BStr)] out string lineString,
      out int cSegment,
      out int[] strokeIdOrdered,
      out int[] segmentStrokes,
      [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr)] out string[] aSegmentString);

    public enum GRAPHICSMODE
    {
      GM_COMPATIBLE = 1,
      GM_ADVANCED = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    public class XFORM
    {
      public float eM11 = 1f;
      public float eM12;
      public float eM21;
      public float eM22 = 1f;
      public float eDx;
      public float eDy;
    }
  }
}
