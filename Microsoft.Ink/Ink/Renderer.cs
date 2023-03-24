// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.Renderer
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
  public class Renderer
  {
    internal IInkRenderer m_Renderer;

    internal Renderer(IInkRenderer inkRenderer) => this.m_Renderer = inkRenderer;

    public Renderer() => this.m_Renderer = (IInkRenderer) new InkRendererClass();

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void SetViewTransform(Matrix viewTransform)
    {
      InkTransformClass ViewTransform = (InkTransformClass) null;
      if (viewTransform != null)
      {
        float[] elements = viewTransform.Elements;
        ViewTransform = new InkTransformClass();
        ViewTransform.SetTransform(elements[0], elements[1], elements[2], elements[3], elements[4], elements[5]);
      }
      this.m_Renderer.SetViewTransform((InkTransform) ViewTransform);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void SetObjectTransform(Matrix objectTransform)
    {
      InkTransformClass ObjectTransform = (InkTransformClass) null;
      if (objectTransform != null)
      {
        float[] elements = objectTransform.Elements;
        ObjectTransform = new InkTransformClass();
        ObjectTransform.SetTransform(elements[0], elements[1], elements[2], elements[3], elements[4], elements[5]);
      }
      this.m_Renderer.SetObjectTransform((InkTransform) ObjectTransform);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void GetViewTransform(ref Matrix viewTransform)
    {
      InkTransform ViewTransform = (InkTransform) new InkTransformClass();
      this.m_Renderer.GetViewTransform(ViewTransform);
      viewTransform = new Matrix(ViewTransform.eM11, ViewTransform.eM12, ViewTransform.eM21, ViewTransform.eM22, ViewTransform.eDx, ViewTransform.eDy);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void GetObjectTransform(ref Matrix objectTransform)
    {
      InkTransform ObjectTransform = (InkTransform) new InkTransformClass();
      this.m_Renderer.GetObjectTransform(ObjectTransform);
      objectTransform = new Matrix(ObjectTransform.eM11, ObjectTransform.eM12, ObjectTransform.eM21, ObjectTransform.eM22, ObjectTransform.eDx, ObjectTransform.eDy);
    }

    public void PixelToInkSpace(Graphics g, ref Point pt)
    {
      IntPtr hdc = g != null ? g.GetHdc() : throw new ArgumentNullException(nameof (g), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      try
      {
        IntSecurity.UnmanagedCode.Assert();
        this.PixelToInkSpace(hdc, ref pt);
      }
      finally
      {
        g.ReleaseHdc(hdc);
      }
    }

    [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public void PixelToInkSpace(IntPtr hdc, ref Point pt)
    {
      int x = pt.X;
      int y = pt.Y;
      this.m_Renderer.PixelToInkSpace((long) (int) hdc, ref x, ref y);
      pt.X = x;
      pt.Y = y;
    }

    public void InkSpaceToPixel(Graphics g, ref Point pt)
    {
      IntPtr hdc = g != null ? g.GetHdc() : throw new ArgumentNullException(nameof (g), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      try
      {
        IntSecurity.UnmanagedCode.Assert();
        this.InkSpaceToPixel(hdc, ref pt);
      }
      finally
      {
        g.ReleaseHdc(hdc);
      }
    }

    [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public void InkSpaceToPixel(IntPtr hdc, ref Point pt)
    {
      int x = pt.X;
      int y = pt.Y;
      this.m_Renderer.InkSpaceToPixel((long) (int) hdc, ref x, ref y);
      pt.X = x;
      pt.Y = y;
    }

    public void PixelToInkSpace(Graphics g, ref Point[] pts)
    {
      IntPtr hdc = g != null ? g.GetHdc() : throw new ArgumentNullException(nameof (g), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      try
      {
        IntSecurity.UnmanagedCode.Assert();
        this.PixelToInkSpace(hdc, ref pts);
      }
      finally
      {
        g.ReleaseHdc(hdc);
      }
    }

    [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public void PixelToInkSpace(IntPtr hdc, ref Point[] pts)
    {
      object coords = (object) InkHelperMethods.PointsToCoords(pts);
      this.m_Renderer.PixelToInkSpaceFromPoints((long) (int) hdc, ref coords);
      pts = InkHelperMethods.CoordsToPoints((int[]) coords);
    }

    public void InkSpaceToPixel(Graphics g, ref Point[] pts)
    {
      IntPtr hdc = g != null ? g.GetHdc() : throw new ArgumentNullException(nameof (g), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      try
      {
        IntSecurity.UnmanagedCode.Assert();
        this.InkSpaceToPixel(hdc, ref pts);
      }
      finally
      {
        g.ReleaseHdc(hdc);
      }
    }

    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    public void InkSpaceToPixel(IntPtr hdc, ref Point[] pts)
    {
      object coords = (object) InkHelperMethods.PointsToCoords(pts);
      this.m_Renderer.InkSpaceToPixelFromPoints((long) (int) hdc, ref coords);
      pts = InkHelperMethods.CoordsToPoints((int[]) coords);
    }

    public void Draw(Graphics g, Strokes strokes)
    {
      if (g == null)
        return;
      int savedDC;
      IntPtr hregion;
      IntPtr fullHdc = Renderer.GetFullHdc(g, out savedDC, out hregion, (object) this);
      try
      {
        IntSecurity.UnmanagedCode.Assert();
        this.Draw(fullHdc, strokes);
      }
      finally
      {
        if (savedDC != 0)
          UnsafeNativeMethods.RestoreDC(fullHdc, savedDC);
        if (hregion != IntPtr.Zero)
          SafeNativeMethods.DeleteObject(new HandleRef((object) this, hregion));
        g.ReleaseHdc(fullHdc);
      }
    }

    [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public void Draw(IntPtr hdc, Strokes strokes)
    {
      if (strokes == null)
        throw new ArgumentNullException(nameof (strokes));
      try
      {
        this.m_Renderer.Draw((long) (int) hdc, strokes.m_Strokes);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public void Draw(Graphics g, Stroke stroke) => this.Draw(g, stroke, (DrawingAttributes) null);

    public void Draw(Graphics g, Stroke stroke, DrawingAttributes da)
    {
      if (g == null)
        return;
      int savedDC;
      IntPtr hregion;
      IntPtr fullHdc = Renderer.GetFullHdc(g, out savedDC, out hregion, (object) this);
      try
      {
        IntSecurity.UnmanagedCode.Assert();
        this.Draw(fullHdc, stroke, da);
      }
      finally
      {
        if (savedDC != 0)
          UnsafeNativeMethods.RestoreDC(fullHdc, savedDC);
        if (hregion != IntPtr.Zero)
          SafeNativeMethods.DeleteObject(new HandleRef((object) this, hregion));
        g.ReleaseHdc(fullHdc);
      }
    }

    public void Draw(IntPtr hdc, Stroke stroke) => this.Draw(hdc, stroke, (DrawingAttributes) null);

    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    public void Draw(IntPtr hdc, Stroke stroke, DrawingAttributes da)
    {
      if (stroke == null)
        throw new ArgumentNullException(nameof (stroke));
      try
      {
        if (da == null)
          this.m_Renderer.DrawStroke((long) (int) hdc, stroke.m_Stroke);
        else
          this.m_Renderer.DrawStroke((long) (int) hdc, stroke.m_Stroke, (InkDrawingAttributes) da.m_DrawingAttributes);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public void Draw(Bitmap destinationBitmap, Strokes strokes)
    {
      if (strokes == null)
        throw new ArgumentNullException(nameof (strokes));
      this.RenderToBitmap(destinationBitmap, strokes, (Stroke) null, (DrawingAttributes) null);
    }

    public void Draw(Bitmap destinationBitmap, Stroke stroke) => this.Draw(destinationBitmap, stroke, (DrawingAttributes) null);

    public void Draw(Bitmap destinationBitmap, Stroke stroke, DrawingAttributes da)
    {
      if (stroke == null)
        throw new ArgumentNullException(nameof (stroke));
      this.RenderToBitmap(destinationBitmap, (Strokes) null, stroke, da);
    }

    private void RenderToBitmap(
      Bitmap destinationBitmap,
      Strokes strokes,
      Stroke stroke,
      DrawingAttributes da)
    {
      if (destinationBitmap == null)
        throw new ArgumentNullException(nameof (destinationBitmap));
      using (Bitmap bitmap = new Bitmap(destinationBitmap.Width, destinationBitmap.Height, PixelFormat.Format32bppPArgb))
      {
        using (Graphics graphics = Graphics.FromImage((Image) bitmap))
        {
          graphics.CompositingMode = CompositingMode.SourceCopy;
          graphics.DrawImage((Image) destinationBitmap, 0, 0);
        }
        IntPtr hbitmap = bitmap.GetHbitmap();
        try
        {
          IntPtr compatibleDc = UnsafeNativeMethods.CreateCompatibleDC(IntPtr.Zero);
          int lastWin32Error1 = Marshal.GetLastWin32Error();
          if (compatibleDc == IntPtr.Zero)
            throw new Win32Exception(lastWin32Error1, Helpers.SharedResources.Errors.GetString("GDIInternalError"));
          try
          {
            BitmapData bitmapdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
            try
            {
              NativeMethods.BITMAPINFO bmi = new NativeMethods.BITMAPINFO();
              bmi.bmiHeader_biSize = 40;
              bmi.bmiHeader_biBitCount = (short) 32;
              bmi.bmiHeader_biPlanes = (short) 1;
              bmi.bmiHeader_biWidth = bitmap.Width;
              bmi.bmiHeader_biHeight = -bitmap.Height;
              int num1 = SafeNativeMethods.SetDIBits(compatibleDc, hbitmap, 0, bitmap.Height, bitmapdata.Scan0, bmi, 0);
              int lastWin32Error2 = Marshal.GetLastWin32Error();
              if (num1 == 0)
                throw new Win32Exception(lastWin32Error2, Helpers.SharedResources.Errors.GetString("GDIInternalError"));
              IntPtr num2 = SafeNativeMethods.SelectObject(compatibleDc, hbitmap);
              try
              {
                IntSecurity.UnmanagedCode.Assert();
                if (strokes != null)
                  this.Draw(compatibleDc, strokes);
                else
                  this.Draw(compatibleDc, stroke, da);
                CodeAccessPermission.RevertAssert();
              }
              finally
              {
                SafeNativeMethods.SelectObject(compatibleDc, num2);
              }
              int diBits = SafeNativeMethods.GetDIBits(compatibleDc, hbitmap, 0, bitmap.Height, bitmapdata.Scan0, bmi, 0);
              int lastWin32Error3 = Marshal.GetLastWin32Error();
              if (diBits == 0)
                throw new Win32Exception(lastWin32Error3, Helpers.SharedResources.Errors.GetString("GDIInternalError"));
            }
            finally
            {
              bitmap.UnlockBits(bitmapdata);
            }
          }
          finally
          {
            UnsafeNativeMethods.DeleteDC(compatibleDc);
          }
        }
        finally
        {
          SafeNativeMethods.DeleteObject(new HandleRef((object) this, hbitmap));
        }
        using (Graphics graphics = Graphics.FromImage((Image) destinationBitmap))
        {
          graphics.CompositingMode = CompositingMode.SourceCopy;
          graphics.DrawImage((Image) bitmap, 0, 0);
        }
      }
    }

    public Rectangle Measure(Strokes strokes)
    {
      if (strokes == null)
        throw new ArgumentNullException(nameof (strokes));
      try
      {
        IInkRectangle inkRectangle = (IInkRectangle) this.m_Renderer.Measure(strokes.m_Strokes);
        return new Rectangle(inkRectangle.Left, inkRectangle.Top, inkRectangle.Right - inkRectangle.Left, inkRectangle.Bottom - inkRectangle.Top);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public Rectangle Measure(Stroke stroke) => this.Measure(stroke, (DrawingAttributes) null);

    public Rectangle Measure(Stroke stroke, DrawingAttributes da)
    {
      if (stroke == null)
        throw new ArgumentNullException(nameof (stroke));
      try
      {
        IInkRectangle inkRectangle = da != null ? (IInkRectangle) this.m_Renderer.MeasureStroke(stroke.m_Stroke, (InkDrawingAttributes) da.m_DrawingAttributes) : (IInkRectangle) this.m_Renderer.MeasureStroke(stroke.m_Stroke);
        return new Rectangle(inkRectangle.Left, inkRectangle.Top, inkRectangle.Right - inkRectangle.Left, inkRectangle.Bottom - inkRectangle.Top);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public void Move(float offsetX, float offsetY) => this.m_Renderer.Move(offsetX, offsetY);

    public void Rotate(float degrees, Point point) => this.m_Renderer.Rotate(degrees, (float) point.X, (float) point.Y);

    public void Rotate(float degrees) => this.m_Renderer.Rotate(degrees);

    public void Scale(float scaleX, float scaleY, bool applyOnPenWidth) => this.m_Renderer.ScaleTransform(scaleX, scaleY, applyOnPenWidth);

    public void Scale(float scaleX, float scaleY) => this.m_Renderer.ScaleTransform(scaleX, scaleY);

    internal static IntPtr GetFullHdc(
      Graphics g,
      out int savedDC,
      out IntPtr hregion,
      object caller)
    {
      savedDC = 0;
      hregion = IntPtr.Zero;
      Region clip = g.Clip;
      if (clip.IsInfinite(g) || !g.Transform.IsIdentity)
        return g.GetHdc();
      hregion = clip.GetHrgn(g);
      IntPtr hdc = g.GetHdc();
      savedDC = UnsafeNativeMethods.SaveDC(hdc);
      int num = savedDC;
      SafeNativeMethods.SelectClipRgn(new HandleRef(caller, hdc), new HandleRef(caller, hregion));
      return hdc;
    }
  }
}
