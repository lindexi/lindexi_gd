// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.TabletPropertyDescriptionCollection
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Collections;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class TabletPropertyDescriptionCollection : CollectionBase
  {
    private float m_inkToDeviceScaleX;
    private float m_inkToDeviceScaleY;

    public TabletPropertyDescriptionCollection()
    {
      this.m_inkToDeviceScaleX = 1f;
      this.m_inkToDeviceScaleY = 1f;
    }

    public TabletPropertyDescriptionCollection(float inkToDeviceScaleX, float inkToDeviceScaleY)
    {
      this.m_inkToDeviceScaleX = (double) inkToDeviceScaleX != 0.0 && (double) inkToDeviceScaleY != 0.0 ? inkToDeviceScaleX : throw new ArgumentException(Helpers.SharedResources.Errors.GetString("InvalidScaleFactor"));
      this.m_inkToDeviceScaleY = inkToDeviceScaleY;
    }

    public float InkToDeviceScaleX => this.m_inkToDeviceScaleX;

    public float InkToDeviceScaleY => this.m_inkToDeviceScaleY;

    public int Add(TabletPropertyDescription value) => !this.List.Contains((object) value) ? this.List.Add((object) value) : throw new ArgumentException(Helpers.SharedResources.Errors.GetString("DuplicateValueInCollection"));

    public void Remove(TabletPropertyDescription value) => this.List.Remove((object) value);

    public TabletPropertyDescription this[int index]
    {
      get => (TabletPropertyDescription) this.List[index];
      set
      {
        if (this.List.Contains((object) value) && this.List.IndexOf((object) value) != index)
          throw new ArgumentException(Helpers.SharedResources.Errors.GetString("DuplicateValueInCollection"));
        this.List[index] = (object) value;
      }
    }
  }
}
