// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DrawingAttributes
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Drawing;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class DrawingAttributes : ICloneable
  {
    internal IInkDrawingAttributes m_DrawingAttributes;

    internal DrawingAttributes(_InternalDrawingAttributes drawingAttributes) => this.m_DrawingAttributes = drawingAttributes.m_DrawingAttributes;

    public DrawingAttributes() => this.m_DrawingAttributes = (IInkDrawingAttributes) new InkDrawingAttributesClass();

    public DrawingAttributes(Color color)
      : this()
    {
      this.Color = color;
    }

    public DrawingAttributes(float width)
      : this()
    {
      this.Width = width;
    }

    public DrawingAttributes(Pen pen)
      : this()
    {
      this.Color = pen != null ? pen.Color : throw new ArgumentNullException(nameof (pen), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      this.Width = pen.Width;
    }

    public Color Color
    {
      get => ColorTranslator.FromOle(this.m_DrawingAttributes.Color);
      set => this.m_DrawingAttributes.Color = ColorTranslator.ToOle(value);
    }

    public bool AntiAliased
    {
      get => this.m_DrawingAttributes.AntiAliased;
      set => this.m_DrawingAttributes.AntiAliased = value;
    }

    public float Width
    {
      get => this.m_DrawingAttributes.Width;
      set => this.m_DrawingAttributes.Width = value;
    }

    public float Height
    {
      get => this.m_DrawingAttributes.Height;
      set => this.m_DrawingAttributes.Height = value;
    }

    object ICloneable.Clone() => (object) this.Clone();

    public DrawingAttributes Clone() => new DrawingAttributes(new _InternalDrawingAttributes((IInkDrawingAttributes) this.m_DrawingAttributes.Clone()));

    public bool FitToCurve
    {
      get => this.m_DrawingAttributes.FitToCurve;
      set => this.m_DrawingAttributes.FitToCurve = value;
    }

    public bool IgnorePressure
    {
      get => this.m_DrawingAttributes.IgnorePressure;
      set => this.m_DrawingAttributes.IgnorePressure = value;
    }

    public byte Transparency
    {
      get => (byte) this.m_DrawingAttributes.Transparency;
      set => this.m_DrawingAttributes.Transparency = (int) value;
    }

    public RasterOperation RasterOperation
    {
      get => (RasterOperation) this.m_DrawingAttributes.RasterOperation;
      set => this.m_DrawingAttributes.RasterOperation = (InkRasterOperation) value;
    }

    public PenTip PenTip
    {
      get => (PenTip) this.m_DrawingAttributes.PenTip;
      set => this.m_DrawingAttributes.PenTip = (InkPenTip) value;
    }

    public ExtendedProperties ExtendedProperties => new ExtendedProperties(this.m_DrawingAttributes.ExtendedProperties, ExtendedProperty.PropertyType.DrawingAttributes);
  }
}
