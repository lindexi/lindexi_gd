// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.Ink
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class Ink : ICloneable, IDisposable
  {
    public static readonly string InkSerializedFormat = "Ink Serialized Format";
    private static readonly Guid IDataObjectGuid = new Guid("{0000010E-0000-0000-C000-000000000046}");
    internal InkDisp m_Ink;
    private CustomStrokes m_CustomStrokes;
    private StrokesEventHandler onInkAdded;
    private StrokesEventHandler onInkDeleted;
    private bool disposed;

    internal Ink(InkDisp ink) => this.m_Ink = ink;

    public Ink() => this.m_Ink = (InkDisp) new InkDispClass();

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    protected virtual void Dispose(bool disposing)
    {
      if (!this.disposed && disposing)
      {
        if (this.m_Ink != null)
        {
          PermissionSet permissionSet = new PermissionSet(PermissionState.None);
          permissionSet.AddPermission((IPermission) new SecurityPermission(SecurityPermissionFlag.UnmanagedCode));
          permissionSet.Assert();
          if (this.onInkAdded != null)
          {
            // ISSUE: method pointer
            this.m_Ink.remove_InkAdded(new _IInkEvents_InkAddedEventHandler((object) this, (UIntPtr) __methodptr(m_Ink_InkAdded)));
          }
          if (this.onInkDeleted != null)
          {
            // ISSUE: method pointer
            this.m_Ink.remove_InkDeleted(new _IInkEvents_InkDeletedEventHandler((object) this, (UIntPtr) __methodptr(m_Ink_InkDeleted)));
          }
          Marshal.ReleaseComObject((object) this.m_Ink);
          CodeAccessPermission.RevertAssert();
        }
        this.m_Ink = (InkDisp) null;
      }
      this.disposed = true;
    }

    ~Ink() => this.Dispose(false);

    public event StrokesEventHandler InkAdded
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onInkAdded += value;
        if (value == null || this.onInkAdded.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Ink.add_InkAdded(new _IInkEvents_InkAddedEventHandler((object) this, (UIntPtr) __methodptr(m_Ink_InkAdded)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onInkAdded == null)
          return;
        this.onInkAdded -= value;
        if (this.onInkAdded != null || this.m_Ink == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Ink.remove_InkAdded(new _IInkEvents_InkAddedEventHandler((object) this, (UIntPtr) __methodptr(m_Ink_InkAdded)));
      }
    }

    public event StrokesEventHandler InkDeleted
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onInkDeleted += value;
        if (value == null || this.onInkDeleted.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Ink.add_InkDeleted(new _IInkEvents_InkDeletedEventHandler((object) this, (UIntPtr) __methodptr(m_Ink_InkDeleted)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onInkDeleted == null)
          return;
        this.onInkDeleted -= value;
        if (this.onInkDeleted != null || this.m_Ink == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Ink.remove_InkDeleted(new _IInkEvents_InkDeletedEventHandler((object) this, (UIntPtr) __methodptr(m_Ink_InkDeleted)));
      }
    }

    object ICloneable.Clone() => (object) this.Clone();

    public Microsoft.Ink.Ink Clone()
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      return new Microsoft.Ink.Ink(this.m_Ink.Clone());
    }

    public Strokes HitTest(Rectangle selectionRectangle, float percentIntersect)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      InkRectangle SelectionRectangle = (InkRectangle) new InkRectangleClass();
      SelectionRectangle.SetRectangle(selectionRectangle.Top, selectionRectangle.Left, selectionRectangle.Bottom, selectionRectangle.Right);
      return new Strokes(this.m_Ink.HitTestWithRectangle(SelectionRectangle, percentIntersect));
    }

    public Strokes HitTest(Point[] points, float percentIntersect, out Point[] lassoPoints)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      object LassoPoints = (object) null;
      InkStrokes strokes = this.m_Ink.HitTestWithLasso((object) InkHelperMethods.PointsToCoords(points), percentIntersect, ref LassoPoints);
      lassoPoints = InkHelperMethods.CoordsToPoints((int[]) LassoPoints);
      return new Strokes(strokes);
    }

    public Strokes HitTest(Point[] points, float percentIntersect)
    {
      Point[] lassoPoints = (Point[]) null;
      return this.HitTest(points, percentIntersect, out lassoPoints);
    }

    public Strokes HitTest(Point point, float radius)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      return new Strokes(this.m_Ink.HitTestCircle(point.X, point.Y, radius));
    }

    public Stroke NearestPoint(Point point)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      float PointOnStroke = 0.0f;
      float DistanceFromPacket = 0.0f;
      IInkStrokeDisp stroke = this.m_Ink.NearestPoint(point.X, point.Y, ref PointOnStroke, ref DistanceFromPacket);
      return stroke != null ? new Stroke(stroke) : (Stroke) null;
    }

    public Stroke NearestPoint(Point point, out float pointOnStroke)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      float PointOnStroke = 0.0f;
      float DistanceFromPacket = 0.0f;
      IInkStrokeDisp stroke = this.m_Ink.NearestPoint(point.X, point.Y, ref PointOnStroke, ref DistanceFromPacket);
      pointOnStroke = PointOnStroke;
      return stroke != null ? new Stroke(stroke) : (Stroke) null;
    }

    public Stroke NearestPoint(Point point, out float pointOnStroke, out float distanceFromPacket)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      float PointOnStroke = 0.0f;
      float DistanceFromPacket = 0.0f;
      IInkStrokeDisp stroke = this.m_Ink.NearestPoint(point.X, point.Y, ref PointOnStroke, ref DistanceFromPacket);
      pointOnStroke = PointOnStroke;
      distanceFromPacket = DistanceFromPacket;
      return stroke != null ? new Stroke(stroke) : (Stroke) null;
    }

    public Strokes Strokes
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return new Strokes(this.m_Ink.Strokes);
      }
    }

    public CustomStrokes CustomStrokes
    {
      get
      {
        if (this.m_CustomStrokes == null)
        {
          if (this.disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          this.m_CustomStrokes = new CustomStrokes(this.m_Ink.CustomStrokes);
        }
        return this.m_CustomStrokes;
      }
    }

    public void DeleteStroke(Stroke stroke)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      if (stroke == null)
        throw new ArgumentNullException(nameof (stroke), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      if ((object) stroke.GetType() != (object) typeof (Stroke))
        return;
      try
      {
        this.m_Ink.DeleteStroke(stroke.m_Stroke);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public void DeleteStrokes(Strokes strokes)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      if (strokes == null)
        throw new ArgumentNullException(nameof (strokes), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      try
      {
        this.m_Ink.DeleteStrokes(strokes.m_Strokes);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public void DeleteStrokes()
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      this.m_Ink.DeleteStrokes();
    }

    public Microsoft.Ink.Ink ExtractStrokes(Strokes strokes, ExtractFlags extractionFlags)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      if (strokes == null)
        throw new ArgumentNullException(nameof (strokes), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      try
      {
        return new Microsoft.Ink.Ink(this.m_Ink.ExtractStrokes(strokes.m_Strokes, (InkExtractFlags) extractionFlags));
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public Microsoft.Ink.Ink ExtractStrokes(Strokes strokes) => this.ExtractStrokes(strokes, ExtractFlags.RemoveFromOriginal);

    public Microsoft.Ink.Ink ExtractStrokes()
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      return new Microsoft.Ink.Ink(this.m_Ink.ExtractStrokes());
    }

    public Microsoft.Ink.Ink ExtractStrokes(
      Rectangle extractionRectangle,
      ExtractFlags extractionFlags)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      InkRectangle Rectangle = (InkRectangle) new InkRectangleClass();
      Rectangle.SetRectangle(extractionRectangle.Top, extractionRectangle.Left, extractionRectangle.Bottom, extractionRectangle.Right);
      return new Microsoft.Ink.Ink(this.m_Ink.ExtractWithRectangle(Rectangle, (InkExtractFlags) extractionFlags));
    }

    public Microsoft.Ink.Ink ExtractStrokes(Rectangle extractionRectangle) => this.ExtractStrokes(extractionRectangle, ExtractFlags.RemoveFromOriginal);

    public void Clip(Rectangle r)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      InkRectangle Rectangle = (InkRectangle) new InkRectangleClass();
      Rectangle.SetRectangle(r.Top, r.Left, r.Bottom, r.Right);
      this.m_Ink.Clip(Rectangle);
    }

    public Rectangle GetBoundingBox(BoundingBoxMode mode)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      InkRectangle boundingBox = this.m_Ink.GetBoundingBox((InkBoundingBoxMode) mode);
      return new Rectangle(boundingBox.Left, boundingBox.Top, boundingBox.Right - boundingBox.Left, boundingBox.Bottom - boundingBox.Top);
    }

    public Rectangle GetBoundingBox() => this.GetBoundingBox(BoundingBoxMode.Default);

    public bool Dirty
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Ink.Dirty;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        this.m_Ink.Dirty = value;
      }
    }

    public Strokes CreateStrokes()
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      return new Strokes(this.m_Ink.CreateStrokes((object) null));
    }

    public Strokes CreateStrokes(int[] ids)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      return new Strokes(this.m_Ink.CreateStrokes((object) ids));
    }

    public void AddStrokesAtRectangle(Strokes strokes, Rectangle destinationRectangle)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      if (strokes == null)
        throw new ArgumentNullException(nameof (strokes), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      InkRectangle TargetRectangle = (InkRectangle) new InkRectangleClass();
      TargetRectangle.SetRectangle(destinationRectangle.Top, destinationRectangle.Left, destinationRectangle.Bottom, destinationRectangle.Right);
      if ((object) strokes.GetType() != (object) typeof (Strokes))
        return;
      try
      {
        this.m_Ink.AddStrokesAtRectangle(strokes.m_Strokes, TargetRectangle);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public ExtendedProperties ExtendedProperties
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return new ExtendedProperties(this.m_Ink.ExtendedProperties, ExtendedProperty.PropertyType.Ink);
      }
    }

    public void Load(byte[] inkdata)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      this.m_Ink.Load((object) inkdata);
    }

    public byte[] Save(PersistenceFormat p, CompressionMode c)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      object obj = this.m_Ink.Save((InkPersistenceFormat) p, (InkPersistenceCompressionMode) c);
      if (p != PersistenceFormat.Base64Gif && p != PersistenceFormat.Base64InkSerializedFormat)
        return (byte[]) obj;
      string str = (string) obj;
      int length = str.Length;
      byte[] numArray = new byte[length];
      for (int index = 0; index < length; ++index)
        numArray[index] = (byte) str[index];
      return numArray;
    }

    public byte[] Save(PersistenceFormat p) => this.Save(p, CompressionMode.Default);

    public byte[] Save() => this.Save(PersistenceFormat.InkSerializedFormat, CompressionMode.Default);

    public Stroke CreateStroke(Point[] points)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      return new Stroke(this.m_Ink.CreateStroke((object) InkHelperMethods.PointsToCoords(points), (object) null));
    }

    public Stroke CreateStroke(
      int[] packetData,
      TabletPropertyDescriptionCollection tabletPropertyDescriptionCollection)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      if (packetData == null)
        throw new ArgumentNullException(nameof (packetData), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      if (tabletPropertyDescriptionCollection == null)
        throw new ArgumentNullException(nameof (tabletPropertyDescriptionCollection), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      if (packetData.Length == 0 || tabletPropertyDescriptionCollection.Count == 0)
        throw new ArgumentException(Helpers.SharedResources.Errors.GetString("ValueCannotBeEmptyArray"));
      if (packetData.Length % tabletPropertyDescriptionCollection.Count != 0)
        throw new ArgumentException(Helpers.SharedResources.Errors.GetString("InvalidPacketDataLength"));
      byte[] bytes = InkHelperMethods.TabletPropertyDescriptionToBytes(tabletPropertyDescriptionCollection);
      return new Stroke(this.m_Ink.CreateStroke((object) packetData, (object) bytes));
    }

    public System.Windows.Forms.IDataObject ClipboardCopy(
      InkClipboardFormats formats,
      InkClipboardModes modes)
    {
      return this.ClipboardCopy((Strokes) null, formats, modes);
    }

    public System.Windows.Forms.IDataObject ClipboardCopy(
      Strokes strokes,
      InkClipboardFormats formats,
      InkClipboardModes modes)
    {
      if ((InkClipboardModes.ExtractOnly & modes) == InkClipboardModes.Copy)
      {
        IntSecurity.ClipboardWrite.Demand();
        if (Application.OleRequired() != ApartmentState.STA)
          throw new ThreadStateException(Helpers.SharedResources.Errors.GetString("ThreadMustBeSTA"));
      }
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      try
      {
        object data = (object) this.m_Ink.ClipboardCopy(strokes == null ? (InkStrokes) null : strokes.m_Strokes, (InkClipboardFormatsPrivate) formats, (InkClipboardModesPrivate) modes);
        return data == null ? (System.Windows.Forms.IDataObject) null : (System.Windows.Forms.IDataObject) new DataObject(data);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public System.Windows.Forms.IDataObject ClipboardCopy(
      Rectangle copyRectangle,
      InkClipboardFormats formats,
      InkClipboardModes modes)
    {
      if ((InkClipboardModes.ExtractOnly & modes) == InkClipboardModes.Copy)
      {
        IntSecurity.ClipboardWrite.Demand();
        if (Application.OleRequired() != ApartmentState.STA)
          throw new ThreadStateException(Helpers.SharedResources.Errors.GetString("ThreadMustBeSTA"));
      }
      InkRectangle Rectangle = (InkRectangle) new InkRectangleClass();
      Rectangle.SetRectangle(copyRectangle.Top, copyRectangle.Left, copyRectangle.Bottom, copyRectangle.Right);
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      object data = (object) this.m_Ink.ClipboardCopyWithRectangle(Rectangle, (InkClipboardFormatsPrivate) formats, (InkClipboardModesPrivate) modes);
      return data == null ? (System.Windows.Forms.IDataObject) null : (System.Windows.Forms.IDataObject) new DataObject(data);
    }

    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [UIPermission(SecurityAction.Demand, Clipboard = UIPermissionClipboard.AllClipboard)]
    public bool CanPaste() => this.CanPaste((object) null);

    public bool CanPaste(object dataObject)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      if (dataObject == null)
      {
        IntSecurity.ClipboardRead.Demand();
        if (Application.OleRequired() != ApartmentState.STA)
          throw new ThreadStateException(Helpers.SharedResources.Errors.GetString("ThreadMustBeSTA"));
        return this.m_Ink.CanPaste(IntPtr.Zero);
      }
      if (!(dataObject is DataObject o))
        o = new DataObject(dataObject);
      IntPtr iunknownForObject = Marshal.GetIUnknownForObject((object) o);
      try
      {
        IntPtr ppv = IntPtr.Zero;
        Guid idataObjectGuid = Microsoft.Ink.Ink.IDataObjectGuid;
        Marshal.QueryInterface(iunknownForObject, ref idataObjectGuid, out ppv);
        try
        {
          return this.m_Ink.CanPaste(ppv);
        }
        finally
        {
          Marshal.Release(ppv);
        }
      }
      finally
      {
        Marshal.Release(iunknownForObject);
      }
    }

    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [UIPermission(SecurityAction.Demand, Clipboard = UIPermissionClipboard.AllClipboard)]
    public Strokes ClipboardPaste() => this.ClipboardPaste(new Point(0, 0));

    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [UIPermission(SecurityAction.Demand, Clipboard = UIPermissionClipboard.AllClipboard)]
    public Strokes ClipboardPaste(Point pt) => this.ClipboardPaste(pt, (object) null);

    public Strokes ClipboardPaste(Point pt, object dataObject)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      InkStrokes strokes;
      if (dataObject == null)
      {
        IntSecurity.ClipboardRead.Demand();
        if (Application.OleRequired() != ApartmentState.STA)
          throw new ThreadStateException(Helpers.SharedResources.Errors.GetString("ThreadMustBeSTA"));
        strokes = this.m_Ink.ClipboardPaste(pt.X, pt.Y, IntPtr.Zero);
      }
      else
      {
        if (!(dataObject is DataObject o))
          o = new DataObject(dataObject);
        IntPtr iunknownForObject = Marshal.GetIUnknownForObject((object) o);
        try
        {
          IntPtr ppv = IntPtr.Zero;
          Guid idataObjectGuid = Microsoft.Ink.Ink.IDataObjectGuid;
          Marshal.QueryInterface(iunknownForObject, ref idataObjectGuid, out ppv);
          try
          {
            strokes = this.m_Ink.ClipboardPaste(pt.X, pt.Y, ppv);
          }
          finally
          {
            Marshal.Release(ppv);
          }
        }
        finally
        {
          Marshal.Release(iunknownForObject);
        }
      }
      return strokes == null ? (Strokes) null : new Strokes(strokes);
    }

    private void m_Ink_InkAdded(object o)
    {
      if (this.onInkAdded == null)
        return;
      this.onInkAdded((object) this, new StrokesEventArgs((int[]) o));
    }

    private void m_Ink_InkDeleted(object o)
    {
      if (this.onInkDeleted == null)
        return;
      this.onInkDeleted((object) this, new StrokesEventArgs((int[]) o));
    }
  }
}
