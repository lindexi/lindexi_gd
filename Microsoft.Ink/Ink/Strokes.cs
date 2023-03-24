// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.Strokes
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class Strokes : ICollection, IEnumerable, IDisposable
  {
    private StrokesEventHandler onStrokesAdded;
    private StrokesEventHandler onStrokesRemoved;
    internal InkStrokes m_Strokes;
    private bool disposed;

    internal Strokes(InkStrokes strokes) => this.m_Strokes = strokes;

    private Strokes()
    {
    }

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
        if (this.m_Strokes != null)
        {
          IntSecurity.RemoveComEventHandler.Assert();
          if (this.onStrokesAdded != null)
          {
            // ISSUE: method pointer
            this.m_Strokes.remove_StrokesAdded(new _IInkStrokesEvents_StrokesAddedEventHandler((object) this, (UIntPtr) __methodptr(m_Strokes_StrokesAdded)));
          }
          if (this.onStrokesRemoved != null)
          {
            // ISSUE: method pointer
            this.m_Strokes.remove_StrokesRemoved(new _IInkStrokesEvents_StrokesRemovedEventHandler((object) this, (UIntPtr) __methodptr(m_Strokes_StrokesRemoved)));
          }
          Marshal.ReleaseComObject((object) this.m_Strokes);
        }
        this.m_Strokes = (InkStrokes) null;
      }
      this.disposed = true;
    }

    ~Strokes() => this.Dispose(false);

    public event StrokesEventHandler StrokesAdded
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onStrokesAdded += value;
        if (value == null || this.onStrokesAdded.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Strokes.add_StrokesAdded(new _IInkStrokesEvents_StrokesAddedEventHandler((object) this, (UIntPtr) __methodptr(m_Strokes_StrokesAdded)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onStrokesAdded == null)
          return;
        this.onStrokesAdded -= value;
        if (this.onStrokesAdded != null || this.m_Strokes == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Strokes.remove_StrokesAdded(new _IInkStrokesEvents_StrokesAddedEventHandler((object) this, (UIntPtr) __methodptr(m_Strokes_StrokesAdded)));
      }
    }

    public event StrokesEventHandler StrokesRemoved
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onStrokesRemoved += value;
        if (value == null || this.onStrokesRemoved.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Strokes.add_StrokesRemoved(new _IInkStrokesEvents_StrokesRemovedEventHandler((object) this, (UIntPtr) __methodptr(m_Strokes_StrokesRemoved)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onStrokesRemoved == null)
          return;
        this.onStrokesRemoved -= value;
        if (this.onStrokesRemoved != null || this.m_Strokes == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Strokes.remove_StrokesRemoved(new _IInkStrokesEvents_StrokesRemovedEventHandler((object) this, (UIntPtr) __methodptr(m_Strokes_StrokesRemoved)));
      }
    }

    public int Count
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Strokes.Count;
      }
    }

    public bool IsFixedSize => false;

    public bool IsSynchronized => false;

    public object SyncRoot => (object) this;

    public bool IsReadOnly => false;

    public void Clear()
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      for (int Index = this.m_Strokes.Count - 1; Index >= 0; --Index)
        this.m_Strokes.Remove(this.m_Strokes.Item(Index));
    }

    public int IndexOf(Stroke s)
    {
      if (s == null)
        throw new ArgumentNullException(nameof (s), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      int count = this.Count;
      for (int index = 0; index < count; ++index)
      {
        if (this[index].Id == s.Id)
          return index;
      }
      return -1;
    }

    public bool Contains(Stroke s)
    {
      if (s == null)
        throw new ArgumentNullException(nameof (s), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      int count = this.Count;
      for (int index = 0; index < count; ++index)
      {
        if (this[index].Id == s.Id)
          return true;
      }
      return false;
    }

    public Stroke this[int index]
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return new Stroke(this.m_Strokes.Item(index));
      }
    }

    public int Add(Stroke stroke)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      if (stroke == null)
        throw new ArgumentNullException(nameof (stroke), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      try
      {
        this.m_Strokes.Add(stroke.m_Stroke);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
      return -1;
    }

    public void Add(Strokes strokes)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      if (strokes == null)
        throw new ArgumentNullException(nameof (strokes), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      try
      {
        this.m_Strokes.AddStrokes(strokes.m_Strokes);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public void Remove(Stroke stroke)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      if (stroke == null)
        throw new ArgumentNullException(nameof (stroke), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      try
      {
        this.m_Strokes.Remove(stroke.m_Stroke);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public void Remove(Strokes strokes)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      if (strokes == null)
        throw new ArgumentNullException(nameof (strokes), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      try
      {
        this.m_Strokes.RemoveStrokes(strokes.m_Strokes);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public void RemoveAt(int i) => this.Remove(this[i]);

    public void CopyTo(Array array, int index)
    {
      InkHelperMethods.ValidateCopyToArray(array, index, this.Count);
      int count = this.Count;
      for (int index1 = 0; index1 < count; ++index1)
      {
        array.SetValue((object) new Stroke(this[index1].m_Stroke), index);
        checked { ++index; }
      }
    }

    public Microsoft.Ink.Ink Ink
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return new Microsoft.Ink.Ink(this.m_Strokes.Ink);
      }
    }

    public void ModifyDrawingAttributes(DrawingAttributes da)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      if (da == null)
        throw new ArgumentNullException(nameof (da), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      this.m_Strokes.ModifyDrawingAttributes((InkDrawingAttributes) da.m_DrawingAttributes);
    }

    public Rectangle GetBoundingBox(BoundingBoxMode mode)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      InkRectangle boundingBox = this.m_Strokes.GetBoundingBox((InkBoundingBoxMode) mode);
      return new Rectangle(boundingBox.Left, boundingBox.Top, boundingBox.Right - boundingBox.Left, boundingBox.Bottom - boundingBox.Top);
    }

    public Rectangle GetBoundingBox() => this.GetBoundingBox(BoundingBoxMode.Default);

    public override string ToString()
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      return ((IInkStrokes) this.m_Strokes).ToString();
    }

    public RecognitionResult RecognitionResult
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Strokes.RecognitionResult == null ? (RecognitionResult) null : new RecognitionResult(this.m_Strokes.RecognitionResult);
      }
    }

    public void RemoveRecognitionResult()
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      this.m_Strokes.RemoveRecognitionResult();
    }

    public void Transform(Matrix inkTransform, bool applyOnPenWidth)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      if (inkTransform == null)
        throw new ArgumentNullException(nameof (inkTransform), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      InkTransform Transform = (InkTransform) new InkTransformClass();
      float[] elements = inkTransform.Elements;
      Transform.SetTransform(elements[0], elements[1], elements[2], elements[3], elements[4], elements[5]);
      this.m_Strokes.Transform(Transform, applyOnPenWidth);
    }

    public void Clip(Rectangle r)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      InkRectangle Rectangle = (InkRectangle) new InkRectangleClass();
      Rectangle.SetRectangle(r.Top, r.Left, r.Bottom, r.Right);
      this.m_Strokes.Clip(Rectangle);
    }

    public void Transform(Matrix inkTransform) => this.Transform(inkTransform, false);

    public void ScaleToRectangle(Rectangle scaleRectangle)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      InkRectangle Rectangle = (InkRectangle) new InkRectangleClass();
      Rectangle.SetRectangle(scaleRectangle.Top, scaleRectangle.Left, scaleRectangle.Bottom, scaleRectangle.Right);
      this.m_Strokes.ScaleToRectangle(Rectangle);
    }

    public void Move(float offsetX, float offsetY)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      this.m_Strokes.Move(offsetX, offsetY);
    }

    public void Rotate(float degrees, Point point)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      this.m_Strokes.Rotate(degrees, (float) point.X, (float) point.Y);
    }

    public void Scale(float scaleX, float scaleY)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      this.m_Strokes.ScaleTransform(scaleX, scaleY);
    }

    public void Shear(float shearX, float shearY)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      this.m_Strokes.Shear(shearX, shearY);
    }

    public Strokes.StrokesEnumerator GetEnumerator() => new Strokes.StrokesEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) new Strokes.StrokesEnumerator(this);

    private void m_Strokes_StrokesAdded(object ids)
    {
      if (this.onStrokesAdded == null)
        return;
      this.onStrokesAdded((object) this, new StrokesEventArgs((int[]) ids));
    }

    private void m_Strokes_StrokesRemoved(object ids)
    {
      if (this.onStrokesRemoved == null)
        return;
      this.onStrokesRemoved((object) this, new StrokesEventArgs((int[]) ids));
    }

    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public class StrokesEnumerator : IEnumerator
    {
      private int position = -1;
      private Stroke[] s;

      public StrokesEnumerator(Strokes s)
      {
        this.s = s != null ? new Stroke[s.Count] : throw new ArgumentNullException(nameof (s), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
        for (int index = 0; index < s.Count; ++index)
          this.s[index] = s[index];
      }

      public bool MoveNext()
      {
        if (this.position >= this.s.Length - 1)
          return false;
        ++this.position;
        return true;
      }

      public void Reset() => this.position = -1;

      object IEnumerator.Current => (object) this.Current;

      public Stroke Current => this.s[this.position];
    }
  }
}
