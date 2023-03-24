// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.Tablet
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Drawing;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class Tablet
  {
    internal IInkTablet m_Tablet;
    internal IInkTablet2 m_Tablet2;
    internal IInkTablet3 m_Tablet3;

    internal Tablet(IInkTablet tablet) => this.m_Tablet = tablet;

    private Tablet()
    {
    }

    public string Name => this.m_Tablet.Name;

    public string PlugAndPlayId => this.m_Tablet.PlugAndPlayId;

    public Rectangle MaximumInputRectangle
    {
      get
      {
        int Top;
        int Left;
        int Bottom;
        int Right;
        this.m_Tablet.MaximumInputRectangle.GetRectangle(out Top, out Left, out Bottom, out Right);
        return new Rectangle(Left, Top, Right - Left, Bottom - Top);
      }
    }

    public TabletHardwareCapabilities HardwareCapabilities => (TabletHardwareCapabilities) this.m_Tablet.HardwareCapabilities;

    public TabletDeviceKind DeviceKind
    {
      get
      {
        if (this.m_Tablet2 == null)
          this.m_Tablet2 = (IInkTablet2) this.m_Tablet;
        return (TabletDeviceKind) this.m_Tablet2.DeviceKind;
      }
    }

    public bool IsMultiTouch
    {
      get
      {
        if (this.m_Tablet3 == null)
          this.m_Tablet3 = (IInkTablet3) this.m_Tablet;
        return this.m_Tablet3.IsMultiTouch;
      }
    }

    public int MaximumCursors
    {
      get
      {
        if (this.m_Tablet3 == null)
          this.m_Tablet3 = (IInkTablet3) this.m_Tablet;
        return (int) this.m_Tablet3.MaximumCursors;
      }
    }

    public override string ToString() => this.Name;

    public bool IsPacketPropertySupported(Guid id) => this.m_Tablet.IsPacketPropertySupported(id.ToString("B"));

    public TabletPropertyMetrics GetPropertyMetrics(Guid id)
    {
      TabletPropertyMetrics propertyMetrics;
      TabletPropertyMetricUnitPrivate Units;
      this.m_Tablet.GetPropertyMetrics(id.ToString("B"), out propertyMetrics.Minimum, out propertyMetrics.Maximum, out Units, out propertyMetrics.Resolution);
      propertyMetrics.Units = (TabletPropertyMetricUnit) Units;
      return propertyMetrics;
    }
  }
}
