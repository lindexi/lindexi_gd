// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.Stroke
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class Stroke
  {
    internal IInkStrokeDisp m_Stroke;

    internal Stroke(IInkStrokeDisp stroke) => this.m_Stroke = stroke;

    private Stroke()
    {
    }

    public int Id => this.m_Stroke.Id;

    public bool Deleted => this.m_Stroke.Deleted;

    public Rectangle GetBoundingBox(BoundingBoxMode mode)
    {
      try
      {
        InkRectangle boundingBox = this.m_Stroke.GetBoundingBox((InkBoundingBoxMode) mode);
        return new Rectangle(boundingBox.Left, boundingBox.Top, boundingBox.Right - boundingBox.Left, boundingBox.Bottom - boundingBox.Top);
      }
      catch (COMException ex)
      {
        if (ex.ErrorCode == -2147220958)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("InvalidStroke"));
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public Rectangle GetBoundingBox() => this.GetBoundingBox(BoundingBoxMode.Default);

    public int[] PolylineCusps => (int[]) this.m_Stroke.PolylineCusps;

    public int[] BezierCusps => (int[]) this.m_Stroke.BezierCusps;

    public Point[] BezierPoints => InkHelperMethods.CoordsToPoints((int[]) this.m_Stroke.BezierPoints);

    public Point[] GetFlattenedBezierPoints(int fittingError) => InkHelperMethods.CoordsToPoints((int[]) this.m_Stroke.GetFlattenedBezierPoints(fittingError));

    public Point[] GetFlattenedBezierPoints() => this.GetFlattenedBezierPoints(0);

    public float[] SelfIntersections => (float[]) this.m_Stroke.SelfIntersections;

    public int PacketCount => this.m_Stroke.PacketCount;

    public int PacketSize => this.m_Stroke.PacketSize;

    public Guid[] PacketDescription
    {
      get
      {
        string[] packetDescription1 = (string[]) this.m_Stroke.PacketDescription;
        Guid[] packetDescription2 = new Guid[packetDescription1.Length];
        for (int index = 0; index < packetDescription1.Length; ++index)
          packetDescription2[index] = new Guid(packetDescription1[index]);
        return packetDescription2;
      }
    }

    public Point[] GetPoints(int index, int count) => InkHelperMethods.CoordsToPoints((int[]) this.m_Stroke.GetPoints(index, count));

    public Point GetPoint(int index) => InkHelperMethods.CoordsToPoints((int[]) this.m_Stroke.GetPoints(index, 1))[0];

    public Point[] GetPoints() => this.GetPoints(0, -1);

    public TabletPropertyMetrics GetPacketDescriptionPropertyMetrics(Guid id)
    {
      TabletPropertyMetrics descriptionPropertyMetrics;
      TabletPropertyMetricUnitPrivate Units;
      this.m_Stroke.GetPacketDescriptionPropertyMetrics(id.ToString("B"), out descriptionPropertyMetrics.Minimum, out descriptionPropertyMetrics.Maximum, out Units, out descriptionPropertyMetrics.Resolution);
      descriptionPropertyMetrics.Units = (TabletPropertyMetricUnit) Units;
      return descriptionPropertyMetrics;
    }

    public int SetPoints(int index, Point[] points)
    {
      int[] coords = InkHelperMethods.PointsToCoords(points);
      return this.m_Stroke.SetPoints((object) coords, index, coords.Length);
    }

    public int SetPoint(int index, Point point) => this.m_Stroke.SetPoints((object) new int[2]
    {
      point.X,
      point.Y
    }, index, 1);

    public int SetPoints(Point[] points) => this.SetPoints(0, points);

    public int[] GetPacketData(int index, int count) => (int[]) this.m_Stroke.GetPacketData(index, count);

    public int[] GetPacketData(int index) => (int[]) this.m_Stroke.GetPacketData(index, 1);

    public int[] GetPacketData() => this.GetPacketData(0, -1);

    public int[] GetPacketValuesByProperty(Guid id, int index, int count) => (int[]) this.m_Stroke.GetPacketValuesByProperty(id.ToString("B"), index, count);

    public int[] GetPacketValuesByProperty(Guid id, int index) => (int[]) this.m_Stroke.GetPacketValuesByProperty(id.ToString("B"), index, 1);

    public int[] GetPacketValuesByProperty(Guid id) => this.GetPacketValuesByProperty(id, 0, -1);

    public int SetPacketValuesByProperty(Guid id, int index, int count, int[] packetValues) => this.m_Stroke.SetPacketValuesByProperty(id.ToString("B"), (object) packetValues, index, count);

    public int SetPacketValuesByProperty(Guid id, int index, int[] packetValues) => this.m_Stroke.SetPacketValuesByProperty(id.ToString("B"), (object) packetValues, index, 1);

    public int SetPacketValuesByProperty(Guid id, int[] packetValues) => this.SetPacketValuesByProperty(id, 0, -1, packetValues);

    public StrokeIntersection[] GetRectangleIntersections(Rectangle intersectRectangle)
    {
      InkRectangle Rectangle = (InkRectangle) new InkRectangleClass();
      Rectangle.SetRectangle(intersectRectangle.Top, intersectRectangle.Left, intersectRectangle.Bottom, intersectRectangle.Right);
      StrokeIntersection[] rectangleIntersections1 = (StrokeIntersection[]) null;
      float[] rectangleIntersections2 = (float[]) this.m_Stroke.GetRectangleIntersections(Rectangle);
      if (rectangleIntersections2 != null)
      {
        int length = rectangleIntersections2.Length / 2;
        rectangleIntersections1 = new StrokeIntersection[length];
        for (int index = 0; index < length; ++index)
          rectangleIntersections1[index] = new StrokeIntersection(rectangleIntersections2[2 * index], rectangleIntersections2[2 * index + 1]);
      }
      return rectangleIntersections1;
    }

    public float[] FindIntersections(Strokes strokes)
    {
      float[] intersections = (float[]) null;
      if (strokes != null)
      {
        if ((object) strokes.GetType() != (object) typeof (Strokes))
          goto label_4;
      }
      try
      {
        intersections = (float[]) this.m_Stroke.FindIntersections(strokes == null ? (InkStrokes) null : strokes.m_Strokes);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
label_4:
      return intersections;
    }

    public bool HitTest(Point pt, float radius) => this.m_Stroke.HitTestCircle(pt.X, pt.Y, radius);

    public float NearestPoint(Point pt, out float distance)
    {
      distance = 0.0f;
      return this.m_Stroke.NearestPoint(pt.X, pt.Y, ref distance);
    }

    public float NearestPoint(Point pt)
    {
      float Distance = 0.0f;
      return this.m_Stroke.NearestPoint(pt.X, pt.Y, ref Distance);
    }

    public Stroke Split(float findex) => new Stroke(this.m_Stroke.Split(findex));

    public void Clip(Rectangle r)
    {
      InkRectangle Rectangle = (InkRectangle) new InkRectangleClass();
      Rectangle.SetRectangle(r.Top, r.Left, r.Bottom, r.Right);
      this.m_Stroke.Clip(Rectangle);
    }

    public void Transform(Matrix inkTransform, bool applyOnPenWidth)
    {
      if (inkTransform == null)
        throw new ArgumentNullException(nameof (inkTransform), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      InkTransform Transform = (InkTransform) new InkTransformClass();
      float[] elements = inkTransform.Elements;
      Transform.SetTransform(elements[0], elements[1], elements[2], elements[3], elements[4], elements[5]);
      this.m_Stroke.Transform(Transform, applyOnPenWidth);
    }

    public void Transform(Matrix inkTransform) => this.Transform(inkTransform, false);

    public void ScaleToRectangle(Rectangle scaleRectangle)
    {
      InkRectangle Rectangle = (InkRectangle) new InkRectangleClass();
      Rectangle.SetRectangle(scaleRectangle.Top, scaleRectangle.Left, scaleRectangle.Bottom, scaleRectangle.Right);
      this.m_Stroke.ScaleToRectangle(Rectangle);
    }

    public void Move(float offsetX, float offsetY) => this.m_Stroke.Move(offsetX, offsetY);

    public void Rotate(float degrees, Point point) => this.m_Stroke.Rotate(degrees, (float) point.X, (float) point.Y);

    public void Scale(float scaleX, float scaleY) => this.m_Stroke.ScaleTransform(scaleX, scaleY);

    public void Shear(float shearX, float shearY) => this.m_Stroke.Shear(shearX, shearY);

    public DrawingAttributes DrawingAttributes
    {
      get
      {
        lock (this)
        {
          DrawingAttributes drawingAttributes = (DrawingAttributes) null;
          if (!this.m_Stroke.Deleted)
          {
            try
            {
              drawingAttributes = new DrawingAttributes(new _InternalDrawingAttributes((IInkDrawingAttributes) this.m_Stroke.DrawingAttributes));
            }
            catch (COMException ex)
            {
              if (ex.ErrorCode != -2147220958)
              {
                InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
                throw;
              }
            }
          }
          return drawingAttributes;
        }
      }
      set
      {
        lock (this)
        {
          if (value == null)
            throw new ArgumentNullException(nameof (value));
          if (this.m_Stroke.Deleted)
            return;
          try
          {
            this.m_Stroke.DrawingAttributes = (InkDrawingAttributes) value.m_DrawingAttributes;
          }
          catch (COMException ex)
          {
            if (ex.ErrorCode == -2147220958)
              return;
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
        }
      }
    }

    public Microsoft.Ink.Ink Ink => new Microsoft.Ink.Ink(this.m_Stroke.Ink);

    public ExtendedProperties ExtendedProperties => new ExtendedProperties(this.m_Stroke.ExtendedProperties, ExtendedProperty.PropertyType.Stroke);
  }
}
